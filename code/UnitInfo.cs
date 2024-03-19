using Sandbox;
using Sandbox.Citizen;
using System;
using System.Security.Cryptography;

[Icon( "psychology" )]
public sealed class UnitInfo : Component
{
	[Property] public string Name { get; set; }
	/// <summary>
	/// Sets max health and HP regen per second
	/// </summary>
	[Property][Range( 1f, 99f, 1f )] public float Constitution { get; set; } = 5f;
	
	/// <summary>
	/// How many seconds out of combat until HP begins regeneration
	/// </summary>
	[Property][Range( 1f, 10f, 1f )] public float HealthRegenTimer { get; set; } = 10f;
	
	/// <summary>
	/// Currently not implemented aside from UI Testing...
	/// </summary>
	[Property][Range( 1f, 15f, 1f )] public int CurrentLevel { get; set; } = 1;
	[Property][Range( 1f, 100f, 1f )] public int chanceOfModifier { get; set; } = 1;

	[Property] SoundEvent OnHurt { get; set; }

	/// <summary>
	/// Current Health
	/// </summary>
	[Property] public float Health { get; private set; }

	[Property] public bool GetsModifier { get; set; } = false;

	public bool Alive { get; private set; } = true;
	
	public float MaxHealth;
	
	public float HealthRegenAmount;
	
	private TimeSince _lastDamage;
	
	private TimeUntil _nextHeal;

	private int modifier;

	public string modifierName;

	protected override void OnStart()
	{
		InitializeHealth();

		if ( GetsModifier == true )
		{
			modifier = Game.Random.Int( 1, 5 );
			RandomModifier( modifier );
		}
	}

	public void InitializeHealth()
	{
		MaxHealth = Constitution;
		HealthRegenAmount = Constitution * 0.1f;
		Health = MaxHealth;
	}

	public void RandomModifier ( int modifier )
	{
		switch( modifier )
		{
			case 1: modifierName = "Autistic";
				break;
			case 2: modifierName = "Hardy";
				MaxHealth = MaxHealth + (10 * CurrentLevel );
				Health = MaxHealth;
				break;
			case 3: modifierName = "Sneaky";
				break;
			case 4: modifierName = "Cryptic";
				break;
			case 5: modifierName = "Blazing";
				break;
		}
	}
	protected override void OnUpdate()
	{
		// Was our last damage 3 seconds ago, and we're not at max HP, and we're alive?
		if (_lastDamage >= HealthRegenTimer && Health != MaxHealth && Alive )
		{
			// Nextheal is at 0? Takes a second, since once it is, we set the TimeUntil back to 1
			if ( _nextHeal )
			{
				// Calls Damage a healing amount, then sets timer back to 1 second
				Damage( -HealthRegenAmount );
				_nextHeal = 1f;
			}
		}	
	}

	/// <summary>
	/// Damage the unit, clamped, heal if damage dealt is negative
	/// </summary>
	/// <param name="damage"></param>
	public void Damage (float damage )
	{
		// Return if unit is already dead
		if ( !Alive ) return;

		// Set the Health equal to Health - damage, clamped between 0 and MaxHealth
		Health = MathX.Clamp( Health - damage, 0f, MaxHealth );

		// Makes sure you aren't being healed, and then starts the lastDamage count
		if ( damage > 0 )
		{
			_lastDamage = 0f;
			Sound.Play( OnHurt, Transform.Position );
		}

		// If Health reached 0, set Alive to false
		if ( Health <= 0 )
			Kill();

	}

	/// <summary>
	/// Set the HP to 0 and Alive to false, then destroys game object
	/// </summary>
	public void Kill()
	{
		Health = 0f;
		Alive = false;

		GameObject.Destroy();
	}


}
