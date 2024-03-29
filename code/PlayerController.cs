using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Citizen;

public class PlayerController : Component
{
	[Property] public Vector3 Gravity { get; set; } = new( 0f, 0f, 800f );
	
	
	
	public List<CitizenAnimationHelper> Animators { get; private set; } = new();
	public CharacterController CharacterController { get; private set; }
	public Vector3 WishVelocity { get; private set; }
	public SkinnedModelRenderer ModelRenderer { get; private set; }



	[Property] public float StandHeight { get; set; } = 64f;
	[Property] public float DuckHeight { get; set; } = 28f;
	[Property] public GameObject Eye { get; set; }
	[Property] public Action OnJump { get; set; }
	[Property] public Vector3 EyePosition { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] public CameraComponent ViewModelCamera { get; set; }
	[Property] public SoundEvent HurtSound { get; set; }



	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public bool IsCrouching { get; set; }
	[Sync] public bool IsRunning { get; set; }
	


	private RealTimeSince LastGroundedTime { get; set; }
	private RealTimeSince LastUngroundedTime { get; set; }
	private bool WantsToCrouch { get; set; }
	private TimeSince timeSinceStep;
	private Vector3 EyeWorldPosition => Transform.Local.PointToWorld( EyePosition );



	protected override void OnStart()
	{
		Animators.Add( AnimationHelper );

		if ( IsProxy && ViewModelCamera.IsValid() )
		{
			ViewModelCamera.Enabled = false;
		}

		base.OnStart();
	}

	public void ResetViewAngles()
	{
		var rotation = Rotation.Identity;
		EyeAngles = rotation.Angles().WithRoll( 0f );
	}

	protected override void OnAwake()
	{
		base.OnAwake();

		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>( true );

		CharacterController = Components.GetInDescendantsOrSelf<CharacterController>( true );
		CharacterController.IgnoreLayers.Add( "player" );

		if ( CharacterController.IsValid() )
		{
			CharacterController.Height = StandHeight;
		}

		if ( IsProxy )
			return;

		ResetViewAngles();
	}

	protected override void OnEnabled()
	{
		ModelRenderer.OnFootstepEvent += OnEvent;
	}

	private void OnEvent( SceneModel.FootstepEvent e )
	{
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

		var idealEyePos = Eye.Transform.Position;
		var headPosition = Transform.Position + Vector3.Up * CharacterController.Height;
		var headTrace = Scene.Trace.Ray( Transform.Position, headPosition )
			.UsePhysicsWorld()
			.IgnoreGameObjectHierarchy( GameObject )
			.WithAnyTags( "solid" )
			.Run();

		headPosition = headTrace.EndPosition - headTrace.Direction * 2f;

		var trace = Scene.Trace.Ray( headPosition, idealEyePos )
			.UsePhysicsWorld()
			.IgnoreGameObjectHierarchy( GameObject )
			.WithAnyTags( "solid" )
			.Radius( 2f )
			.Run();

			Scene.Camera.Transform.Position = trace.Hit ? trace.EndPosition : idealEyePos;
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			EyeAngles += Input.AnalogLook;
			EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -85f, 85f ) );
			Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

			Eye.Transform.Position = EyeWorldPosition;
			Eye.Transform.LocalRotation = EyeAngles.WithYaw( 0f );

			IsRunning = Input.Down( "Run" );
		}

		foreach ( var animator in Animators )
		{
			// animator.HoldType = weapon.IsValid() ? weapon.HoldType : CitizenAnimationHelper.HoldTypes.None;
			animator.WithVelocity( CharacterController.Velocity );
			animator.WithWishVelocity( WishVelocity );
			animator.IsGrounded = CharacterController.IsOnGround;
			animator.FootShuffle = 0f;
			animator.DuckLevel = IsCrouching ? 1f : 0f;
			animator.WithLook( EyeAngles.Forward );
		}
	}

	private void UpdateModelVisibility()
	{
		if ( !ModelRenderer.IsValid() )
			return;

		ModelRenderer.Enabled = true;

		ModelRenderer.RenderType = IsProxy
			? Sandbox.ModelRenderer.ShadowRenderType.On
			: Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;

	}


	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;


		DoCrouchingInput();
		DoMovementInput();
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
		{
			animator.TriggerJump();
		}

		if ( HurtSound is not null )
		{
			Sound.Play( HurtSound, Transform.Position.WithZ(64f) );
		}

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
}
