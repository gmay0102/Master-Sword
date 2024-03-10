using Sandbox;
using Sandbox.Citizen;
using System.Numerics;

public sealed class OldPlayerController : Component
{
    [Property] [Category( "Components" )] public GameObject Camera {  get; set; }
    [Property] [Category( "Components" )] public CharacterController Controller { get; set; }
    [Property] [Category( "Components" )] public CitizenAnimationHelper Animator { get; set;}
    [Property] [Category( "Stats" )] [Range( 0f, 400f, 10f )] public float WalkSpeed { get; set; } = 150f;
    [Property] [Category( "Stats" )] [Range( 0f, 600f, 10f )] public float RunSpeed { get; set; } = 250f;
	[Property] [Category( "Stats" )] [Range( 0f, 800f, 10f )] public float JumpStrength { get; set; } = 400f;
	[Property][Category( "Stats" )][Range( 1f, 12f, 1f )] public float DamageMax { get; set; } = 6f;
	[Property][Category( "Stats" )][Range( 0.1f, 5f, 0.1f )] public float SwingSpeed { get; set; } = 0.5f;
	[Property][Category( "Stats" )][Range( 10f, 400f, 5f )] public float SwingRange { get; set; } = 50f;


	/// <summary>
	/// What the camera rotates around in Third Person
	/// </summary>
	[Property] public Vector3 EyePosition { get; set; }

	// Because of the =>, every time EyeWorldPosition is called, it is set to the player's local transform to translate
	// the eye position into the world coordinates
	public Vector3 EyeWorldPosition => Transform.Local.PointToWorld( EyePosition );
	
	// Declared now, then OnUpdate will actually fill it in with where we're looking
    public Angles EyeAngles { get; set; }

	[Property] public SkinnedModelRenderer BodyModel {  get; set; }
	[Property] public SkinnedModelRenderer Helmet { get; set; }
	[Property] public SkinnedModelRenderer Armor { get; set; }
	[Property] public UnitInfo myUnitInfo { get; set; }

	/// <summary>
	/// Determines if the player is controller in first or third person
	/// </summary>
	[Property] public bool FirstPerson { get; set; } = false;

	private float FallDamage;
	
	// Declare a private fella that we'll use in OnStart
	Transform _initialCameraTransform;
	
	TimeSince _lastSwing;

	protected override void DrawGizmos()
	{
		// If the Gizmo isn't selected, stop running this code
		if ( !Gizmo.IsSelected ) return;
		
		// Declares draw as Gizmo.Draw, to clean up the code later
		var draw = Gizmo.Draw;
		
		// Draws a Gizmo in the editor for us to move the Vector3 Eyeposition around easily
		draw.LineSphere( EyePosition, 10f );
		// Draws a Cylinder from EyePos, to forward by a length of SwingRange, 5 wide, 5 thick, 10 lines
		draw.LineCylinder( EyePosition, EyePosition + Transform.Rotation.Forward * SwingRange, 5f, 5f, 10 );
	}

	protected override void OnEnabled()
	{
		if ( BodyModel is null )
			return;

		BodyModel.OnFootstepEvent += OnEvent;
	}

	TimeSince timeSinceStep;

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
		
		if (sound is null ) return;

		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		handle.Volume *= e.Volume;

		;
	}

	public void Swing()
	{
		if ( Animator != null )
		{
			Animator.HoldType = CitizenAnimationHelper.HoldTypes.Swing;
			Animator.Target.Set( "b_attack", true );
		}

		var swingTrace = Scene.Trace
			.FromTo( EyeWorldPosition, EyeWorldPosition + EyeAngles.Forward * SwingRange )
			.Size( 10f )
			.WithoutTags( "player" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();


		if ( swingTrace.Hit )
			if ( swingTrace.GameObject.Components.TryGet<UnitInfo>( out var unitInfo ) )
			{
				unitInfo.Damage( 3f );
				if ( unitInfo.Health == 0 )
					myUnitInfo.AwardXP( 5 );
			}



		_lastSwing = 0f;
	}

	protected override void OnStart()
	{
		if ( Camera != null )
			// Set initialCameraTransform to the camera's current local transform, relative to the player it's parented on
			_initialCameraTransform = Camera.Transform.Local;
	}

	protected override void OnUpdate()
	{
		if ( Input.Pressed( "ChangeView" ) )
			FirstPerson = !FirstPerson;

		if ( !IsProxy )
		{
			// Every frame (we're in OnUpdate) we add how far the mouse/stick moved in the last frame
			EyeAngles += Input.AnalogLook;

			// Change EyeAngles into a version of itself that clamps from -85 to 85, so you cant invert the cam looking up
			EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -85f, 85f ) );

			// Transform our game object to have the Yaw of where the player is looking
			Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );
		}

		if ( Camera != null )
		{
			if ( !FirstPerson )
			{
				// cameraTransform is centered around the player's head, transformed by EyeAngles without its Yaw component
				var cameraTransform = _initialCameraTransform.RotateAround( EyePosition, EyeAngles.WithYaw( 0f ) );

				// cameraPosition is a point at the camera's ideal position
				var cameraPosition = Transform.Local.PointToWorld( cameraTransform.Position );

				// Casts a ray between center of head and camera's ideal position
				var cameraTrace = Scene.Trace.Ray( EyeWorldPosition, cameraPosition )
					// Makes the trace 5 units thick, so it doesn't go between small gaps
					.Size( 5f )
					// Ignore our GameObject and its children
					.IgnoreGameObjectHierarchy( GameObject )
					// Also ignore anything with the tag "player"
					.WithoutTags( "player" )
					.Run();

				// We ran the trace, now, place the camera object at the end point of the trace
				Camera.Transform.Position = cameraTrace.EndPosition;

				// And grab the rotation of our cameraTransform variable from earlier to use for the camera
				Camera.Transform.LocalRotation = cameraTransform.Rotation;

				BodyModel.RenderType = ModelRenderer.ShadowRenderType.On;
				Helmet.RenderType = ModelRenderer.ShadowRenderType.On;
				Armor.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				Camera.Transform.Position = EyeWorldPosition;
				Camera.Transform.LocalRotation = EyeAngles.WithYaw( 0f );
				BodyModel.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
				Helmet.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
				//Armor.RenderType = ModelRenderer .ShadowRenderType.ShadowsOnly;
			}
		}

	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		// Simple check that stops us from doing anything else if the Controller doesn't exist, for optimization
		if ( Controller == null ) return;

		// If "Run" being held down is non-null, wishSpeed is equal to RunSpeed, otherwise WalkSpeed
		var wishSpeed = Input.Down( "Run" ) ? RunSpeed : WalkSpeed;
		// Gets velocity vector (normalized) of AnalogMove buffed up by wishSpeed, then transformed by our game object's rotation
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Transform.Rotation;

		// Now we move the Character Controller in the Vector3 direction and speed of wishVelocity
		Controller.Accelerate( wishVelocity );
		// Sets our acceleration back to default
		Controller.Acceleration = 10f;

		if ( Controller.Velocity.z <= FallDamage )
			FallDamage = Controller.Velocity.z;

		Log.Info( $"FallDamage is {FallDamage}" );

		// Check if we're on the ground, and if we are, apply friction
		if ( Controller.IsOnGround )
		{

			if ( FallDamage < -1000 )
			{
				myUnitInfo.Damage( -FallDamage / 100 );
				FallDamage = 0;
			}
			else
				FallDamage = 0;
			// Simply applies friction at a force of 5f
			Controller.ApplyFriction( 5f, 20f );

			// While we're on the ground, check if Jump was pressed in this frame, if so...
			if ( Input.Pressed( "Jump" ) && myUnitInfo.Stamina > 0 )
			{
				// Punches the player (detach from ground and velocity up after), buffed by our JumpStrength variable
				Controller.Punch( Vector3.Up * JumpStrength );

				myUnitInfo.StaminaDrain( .20f );

				// Okay, we're jumping by this point, so let's animate the Animator
				// First, make sure the Animator isn't null, then make it jump
				if ( Animator != null )
					Animator.TriggerJump();
			}
		}
		else
		{
			// Add the gravity of the scene to the velocity every tick to make them fall faster
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
			// We're clearly in the air right now, so we have less acceleration
			Controller.Acceleration = 2f;
		}

		// Does all the complicated math to move the player object and collide with world
		Controller.Move();

		// Updates the Animator every frame based on if we're on the ground and how fast we are
		if ( Animator != null )
		{
			Animator.IsGrounded = Controller.IsOnGround;
			Animator.WithVelocity( Controller.Velocity );

			if ( _lastSwing >= 3f )
				Animator.HoldType = CitizenAnimationHelper.HoldTypes.None;
		}

		if ( Input.Pressed( "attack1" ) && _lastSwing >= SwingSpeed )
		{
			Swing();
		}
	}
}
