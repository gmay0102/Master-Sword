using Sandbox;
using Sandbox.Citizen;

public sealed class EnemyController : Component, Component.ITriggerListener
{
	[Property] NavMeshAgent NavMeshAgent { get; set; }
	public Vector3 targetPosition { get; set; }
	public Vector3 spawnPosition { get; set; }
	public bool imGayNow { get; set; } = false;

	protected override void OnAwake()
	{
		spawnPosition = NavMeshAgent.Transform.LocalPosition;
	}
	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( imGayNow == false)
		{
			foreach ( var player in Scene.GetAllComponents<CharacterController>() )
				targetPosition = player.Transform.LocalPosition;	
		}
		else
		{
			targetPosition = spawnPosition;
		}
		
		NavMeshAgent.MoveTo( targetPosition );
	}

	public void OnTriggerEnter( Collider other )
	{
		var player = other.Components.Get<OldPlayerController>();
		if ( player != null )
		{
			BoarAttack( 5f );
			imGayNow = true;

		}
	}

	public void BoarAttack( float damage )
	{

	}

	public void OnTriggerExit( Collider other )
	{

	}
}
