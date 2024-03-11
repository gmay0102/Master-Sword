using Sandbox;
using Sandbox.Citizen;

public sealed class EnemyController : Component
{
	[Property] NavMeshAgent NavMeshAgent { get; set; }



	public Vector3 spawnPosition { get; set; }
	public PlayerController targetPlayer;
	private PlayerController localPlayer;

	protected override void OnStart()
	{
		spawnPosition = NavMeshAgent.Transform.LocalPosition;
		var players = Game.ActiveScene.GetAllComponents<PlayerController>().ToList();
		targetPlayer = Game.Random.FromList( players );
		Log.Info( $"Targeting {targetPlayer}" );
	}
	protected override void OnUpdate()
	{
		if ( targetPlayer is null ) return;

		if (Vector3.DistanceBetween(targetPlayer.Transform.Position, NavMeshAgent.Transform.Position) < 120f && targetPlayer is not null)
			NavMeshAgent?.Stop();
		else
			NavMeshAgent?.MoveTo(targetPlayer.Transform.Position);

		Log.Info( Vector3.DistanceBetween( targetPlayer.Transform.Position, NavMeshAgent.Transform.Position ));
		
	
	}

	public void BoarAttack( float damage )
	{

	}
}
