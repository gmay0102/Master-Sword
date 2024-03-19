using Sandbox;
using Sandbox.Citizen;

public sealed class EnemyController : Component
{
	[Property] NavMeshAgent NavMeshAgent { get; set; }
	[Property] UnitInfo UnitInfo { get; set; }
	[Property] SoundEvent Squeal {  get; set; }



	public Vector3 spawnPosition { get; set; }
	public PlayerController targetPlayer;
	private PlayerController localPlayer;
	private bool _inAttack = false;

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

		if ( Vector3.DistanceBetween( targetPlayer.Transform.Position, NavMeshAgent.Transform.Position ) < 120f && targetPlayer is not null )
		{
			BoarAttack( 50f );
		}
		else
			NavMeshAgent?.MoveTo( targetPlayer.Transform.Position );
	}

	public void BoarAttack( float damage )
	{
		if ( !_inAttack )
		{
			_inAttack = true;
			NavMeshAgent.Stop();
			GameObject.Transform.Rotation = Rotation.LookAt( targetPlayer.Transform.Position - GameObject.Transform.Position );
			Sound.Play( Squeal, GameObject.Transform.Position );
		}
	}

	public void AmbientMode()
	{ }

}
