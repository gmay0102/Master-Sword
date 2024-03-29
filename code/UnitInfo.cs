using Sandbox;
using Sandbox.Citizen;
using System;
using System.Security.Cryptography;
public enum UnitType
{
	/// <summary>
	/// For environmental units or resources
	/// </summary>
	[Icon( "check_box_outline_blank" )]None,
	/// <summary>
	/// Players
	/// </summary>
	[Icon( "groups" )] Hero,
	/// <summary>
	/// Boars, skeletons, dragons, grrrr
	/// </summary>
	[Icon( "bug_report" )] Enemy
}

[Icon( "psychology" )]
public sealed class UnitInfo : Component
{
	[Property] UnitType Team { get; set; }
	
	/// <summary>
	/// Controls damage and hit chance with STR based weaponry
	/// </summary>
	[Property][Range( 1f, 10f, 1f )] public float Strength { get; set; } = 5f;
	
	/// <summary>
	/// Controls damage and hit chance with DEX based weaponry
	/// </summary>
	[Property][Range( 1f, 10f, 1f )] public float Dexterity { get; set; } = 5f;
	
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

	/// <summary>
	/// Current Health
	/// </summary>
	[Property] public float Health { get; private set; }

	[Property] public float Stamina { get; set; }

	[Property] public bool GetsModifier { get; set; } = false;

	public bool Alive { get; private set; } = true;
	
	public float MaxHealth;

	public float MaxStamina;
	
	public float HealthRegenAmount;
	
	private TimeSince _lastDamage;
	
	private TimeUntil _nextHeal;

	private TimeSince _lastStamina;

	private int modifier;

	public string modifierName;

	private bool StaminaRegen = false;

	public float currentEXP;

	public float nextLevel = 100f;
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
		MaxStamina = Constitution;
		HealthRegenAmount = Constitution * 0.1f;
		Health = MaxHealth;
		Stamina = MaxStamina;
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
			case 5: modifierName = "Gyrating";
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

		if (_lastStamina >= 3f && Stamina != MaxStamina && Alive )
		{
			StaminaRegen = true;
			_lastStamina = 0f;
		}

		if ( StaminaRegen )
			StaminaDrain( -0.005f );
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
			_lastDamage = 0f;

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

	public void LevelUp()
	{
		CurrentLevel += 1;
		Strength += 1;
		Dexterity += 1;
		Constitution += 1;
		InitializeHealth();
	}

	public void AwardXP (float addedXP )
	{
		currentEXP += addedXP;
		Log.Info( currentEXP );
		LevelUp();
	}

	public void StaminaDrain (float staminaPercent )
	{
		if (!Alive ) return;

		if ( Stamina == MaxStamina )
			StaminaRegen = false;

		staminaPercent *= MaxStamina;

		Stamina = MathX.Clamp( Stamina - staminaPercent, 0f, MaxStamina );

		if ( Stamina > 0 )
			_lastStamina = 0f;
	}
}
