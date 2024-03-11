using Sandbox;
using System;

public sealed class EnemySpawner : Component
{
	[Property] public GameObject EnemyPrefab {  get; set; }
	public float GetRandom() => Random.Shared.Float( 1, 100 );
	public EnemyController[] enemies;

	
	protected override void OnUpdate()
	{
		enemies = Scene.GetAllComponents<EnemyController>().ToArray();
	}

	void SpawnEnemy()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		var randomSpawnPoint = Scene.NavMesh.GetRandomPoint().GetValueOrDefault();
		var enemy = EnemyPrefab.Clone( randomSpawnPoint );
		enemy.NetworkSpawn();
	}

	TimeUntil nextSecond = 0f;
	protected override void OnFixedUpdate()
	{
		float time = GetRandom();
		if ( enemies is null ) return;
		if (nextSecond && enemies.Length <= 15 && time > 80f)
		{
			SpawnEnemy();
			nextSecond = 1;
			GetRandom();
		}
	}
}
