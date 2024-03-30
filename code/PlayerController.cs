using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
using Sandbox;
using Sandbox.Citizen;

public class PlayerController : Component
{
	[Property] public Vector3 Gravity { get; set; } = new( 0f, 0f, 800f );
	
	
	
	public List<CitizenAnimationHelper> Animators { get; private set; } = new();
	public CharacterController CharacterController { get; private set; }
	public Vector3 WishVelocity { get; private set; }
	public SkinnedModelRenderer ModelRenderer { get; private set; }


	
	[Property] public GameObject Eye { get; set; }
	[Property] public Action OnJump { get; set; }
	[Property] public Vector3 EyePosition { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] public SoundEvent JumpSound { get; set; }
	[Property] public SoundEvent SwingSound { get; set; }
	[Property] public SoundEvent HitSound { get; set; }
	[Property] public SoundEvent PickUpSound {  get; set; }
	[Property] public PlayerStats PlayerStats { get; set; }



	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public bool IsCrouching { get; set; }
	[Sync] public bool IsRunning { get; set; }
	public float StandHeight { get; set; } = 64f;
	public float DuckHeight { get; set; } = 28f;



	private RealTimeSince LastGroundedTime { get; set; }
	private RealTimeSince LastUngroundedTime { get; set; }
	private bool WantsToCrouch { get; set; }
	private TimeSince timeSinceStep;
	public TimeSince _lastSwing;
	private Vector3 EyeWorldPosition => Transform.Local.PointToWorld( EyePosition );
	private Vector3 CurrentOffset = Vector3.Zero;



	protected override void OnAwake()
	{
		base.OnAwake();

		if ( IsProxy )
			return;

		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>( true );

		CharacterController = Components.GetInDescendantsOrSelf<CharacterController>( true );
		CharacterController.IgnoreLayers.Add( "player" );

		if ( CharacterController.IsValid() )
		{
			CharacterController.Height = StandHeight;
		}

		ResetViewAngles();
	}

	protected override void OnEnabled()
	{
		// Assigns 
		ModelRenderer.OnFootstepEvent += OnEvent;
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy && Eye.IsValid() )
		{
			Eye.Enabled = false;
		}

		Animators.Add( AnimationHelper );

		//var clothing = ClothingContainer.CreateFromLocalUser();
		//clothing.Apply( ModelRenderer );
	}

	private void OnEvent( SceneModel.FootstepEvent e )
	{
		// Fancy code stolen from Facepunch that plays sound effects based on each foot tracing the surface below it

		if ( timeSinceStep < 0.2f )
			return;

		var tr = Scene.Trace
			.Ray( e.Transform.Position + Vector3.Up * 20, e.Transform.Position + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
			return;

		if ( tr.Surface is null )
			return;

		timeSinceStep = 0;

		var sound = e.FootId == 0 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;

		if ( sound is null ) return;

		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		handle.Volume *= e.Volume;

		;
	}

	public void ResetViewAngles()
	{
		var rotation = Rotation.Identity;
		EyeAngles = rotation.Angles().WithRoll( 0f );
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			// Rotates the GameObject based on mouse input
			EyeAngles += Input.AnalogLook;
			EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -85f, 85f ) );
			Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

			// Keeps the camera at the player's head, then rotates it
			Eye.Transform.Position = EyeWorldPosition;
			Eye.Transform.LocalRotation = EyeAngles.WithYaw( 0f );

			// Creates CurrentOffset which Lerps between standheight and duckheight for camera Z position
			var targetOffset = Vector3.Zero;
			if ( IsCrouching ) targetOffset += Vector3.Down * DuckHeight;
			CurrentOffset = Vector3.Lerp( CurrentOffset, targetOffset, Time.Delta * 10f );
			Eye.Transform.Position = Eye.Transform.Position + CurrentOffset;

			// We runnin???
			IsRunning = Input.Down( "Run" );
		}

		foreach ( var animator in Animators )
		{
			animator.WithVelocity( CharacterController.Velocity );
			animator.WithWishVelocity( WishVelocity );
			animator.IsGrounded = CharacterController.IsOnGround;
			animator.DuckLevel = IsCrouching ? 1f : 0f;
			animator.WithLook( EyeAngles.Forward );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		DoCrouchingInput();

		DoMovementInput();

		if ( Input.Pressed( "attack1" ) && _lastSwing >= 0.5f )
			Swing();

		if ( Input.Pressed( "use" ) )
			Interact();

		if ( Input.Pressed( "score" ) )
			Game.Overlay.ShowBinds();
	}

	private void UpdateModelVisibility()
	{
		if ( !ModelRenderer.IsValid() )
			return;	

		ModelRenderer.RenderType = IsProxy
			? Sandbox.ModelRenderer.ShadowRenderType.On
			: Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;

	}

	protected override void OnPreRender()
	{
		base.OnPreRender();

		if ( !Scene.IsValid() || !Scene.Camera.IsValid() )
			return;

		UpdateModelVisibility();

		if ( IsProxy )
			return;

		if ( !Eye.IsValid() )
			return;

	}

	protected virtual void DoCrouchingInput()
	{
		WantsToCrouch = CharacterController.IsOnGround && Input.Down( "Duck" );

		if ( WantsToCrouch == IsCrouching )
			return;

		if ( WantsToCrouch )
		{
			CharacterController.Height = DuckHeight;
			IsCrouching = true;
		}
		else
		{
			if ( !CanUncrouch() )
				return;

			CharacterController.Height = StandHeight;
			IsCrouching = false;
		}
	}

	protected virtual void DoMovementInput()
	{
		BuildWishVelocity();

		if ( CharacterController.IsOnGround && Input.Down( "Jump" ) )
		{
			CharacterController.Punch( Vector3.Up * 300f );
			SendJumpMessage();
		}

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0f );
			CharacterController.Accelerate( WishVelocity );
			CharacterController.ApplyFriction( 4.0f );
		}
		else
		{
			CharacterController.Velocity -= Gravity * Time.Delta * 0.5f;
			CharacterController.Accelerate( WishVelocity.ClampLength( 50f ) );
			CharacterController.ApplyFriction( 0.1f );
		}

		CharacterController.Move();

		if ( !CharacterController.IsOnGround )
		{
			CharacterController.Velocity -= Gravity * Time.Delta * 0.5f;
			LastUngroundedTime = 0f;
		}
		else
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
			LastGroundedTime = 0f;
		}

		Transform.Rotation = Rotation.FromYaw( EyeAngles.ToRotation().Yaw() );
	}

	private void SendJumpMessage()
	{
		foreach ( var animator in Animators )
			animator.TriggerJump();

		if ( JumpSound is not null )
			Sound.Play( JumpSound, Transform.Position.WithZ(64f) );

		OnJump?.Invoke();
	}

	private void BuildWishVelocity()
	{
		var rotation = EyeAngles.ToRotation();

		WishVelocity = rotation * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0f );

		if ( !WishVelocity.IsNearZeroLength )
			WishVelocity = WishVelocity.Normal;

		if ( IsCrouching )
			WishVelocity *= 64f;
		else if ( IsRunning )
			WishVelocity *= 280f;
		else
			WishVelocity *= 180f;
	}

	protected virtual bool CanUncrouch()
	{
		if ( !IsCrouching ) return true;
		if ( LastUngroundedTime < 0.2f ) return false;

		var tr = CharacterController.TraceDirection( Vector3.Up * DuckHeight );
		return !tr.Hit;
	}

	private void Swing()
	{
		if (IsProxy) return;
		
		if ( AnimationHelper != null )
		{
			AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Swing;
			AnimationHelper.Target.Set( "b_attack", true );
			Sound.Play( SwingSound, Transform.Position.WithZ( 64f ) );

		}

		var swingTrace = Scene.Trace
			.FromTo( Eye.Transform.Position, Eye.Transform.Position + EyeAngles.Forward * 100f )
			.Size( 10f )
			.WithoutTags( "player" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( swingTrace.Hit )
			if ( swingTrace.GameObject.Components.TryGet<UnitInfo>( out var unitInfo ) )
			{
				unitInfo.Damage( PlayerStats.Strength );
				Sound.Play( HitSound, Transform.Position.WithZ( 64f ).WithX( 4f) );

				var log = Scene.GetAllComponents<BattleLog>().FirstOrDefault();
				log.AddTextLocal( $"⚔ {PlayerStats.Strength} damage dealt to {unitInfo.modifierName} {unitInfo.Name}" );

				if ( unitInfo.Health == 0 )
					PlayerStats.gainXP( 55f );
			}

		_lastSwing = 0f;
	}

	private void Interact()
	{
		if ( IsProxy ) return;

		var swingTrace = Scene.Trace
			.FromTo( Eye.Transform.Position, Eye.Transform.Position + EyeAngles.Forward * 100f )
			.Size( 5f )
			.WithoutTags( "player" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( swingTrace.Hit )
			if (swingTrace.GameObject.Components.TryGet<ItemStats>( out var itemStats ) )
			{
				Sound.Play( PickUpSound, Transform.Position.WithZ( 64f ) );

				var log = Scene.GetAllComponents<BattleLog>().FirstOrDefault();
				log.AddTextLocal( $"🎒 Obtained {itemStats.ItemRarity} {itemStats.Name}" );
				swingTrace.GameObject.Destroy();
			}


	}
}
