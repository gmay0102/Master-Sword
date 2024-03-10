using Sandbox;
[Icon( "face" )]

public sealed class PlayerStats : Component
{
	// Core player stats
	[Property] [Category("Stats")] public float Strength { get; set; }
	[Property] [Category( "Stats" )] public float Body { get; set; }
	[Property] [Category( "Stats" )] public float Mind { get; set; }


	// Core player resource pools
	[Property] [Category( "Resource" )] public float MaxHealth;
	[Property] [Category( "Resource" )] public float MaxStamina;
	[Property] [Category( "Resource" )] public float MaxMana;



}
