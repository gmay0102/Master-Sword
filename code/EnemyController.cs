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
	private bool _hasTarget = false;

	protected override void OnStart()
	{
		spawnPosition = NavMeshAgent.Transform.LocalPosition;
		//var players = Game.ActiveScene.GetAllComponents<PlayerController>().ToList();
		//targetPlayer = Game.Random.FromList( players );
		//Log.Info( $"Targeting {targetPlayer}" );
	}
	protected override void OnUpdate()
	{
		if ( _hasTarget ) return;

		//var tr = Scene.Trace
		//.Sphere( 32.0f, NavMeshAgent.Transform.Position, NavMeshAgent.Transform.Position + 32f ) // 32 is the radius
		//.WithTag( "player" ) // ignore GameObjects with this tag
		//.Run();

		//if ( tr.Hit )
		//{
		//	Log.Info( $"Hit: {tr.GameObject} at {tr.EndPosition}" );
		//}

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
