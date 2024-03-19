using Sandbox;

public sealed class Fists : Component
{

	[Property] public SkinnedModelRenderer arms {  get; set; }

	private CharacterController characterController;
	private PlayerController playerController;



	protected override void OnStart()
	{
		if ( IsProxy ) return;
		characterController = Game.ActiveScene.GetAllComponents<CharacterController>().FirstOrDefault(x => !x.IsProxy);
		playerController = Game.ActiveScene.GetAllComponents<PlayerController>().FirstOrDefault(x => !x.IsProxy);
	}
	protected override void OnUpdate()
	{
		if (IsProxy ) return;
		Animations();
	}

	void Animations()
	{
		if ( IsProxy ) return;

		if ( Input.Pressed( "jump" ) && !IsProxy )
			arms.Set( "b_jump", true );

		arms.Set( "b_sprint", Input.Down( "run" ));
		arms.Set( "b_attack", Input.Pressed( "attack1" ) && playerController._lastSwing >= 0.5f  );
		arms.Set( "b_grounded", playerController.CharacterController.IsOnGround );

		
	}
}
