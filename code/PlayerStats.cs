using Sandbox;
using Sandbox.UI;
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



	[Property] public SoundEvent levelUpSound;
	[Property] public float Stamina { get; set; }
	[Property] public float Health { get; private set; }
	[Property] public float Mana { get; set; }



	private TimeSince _lastStamina;
	private bool StaminaRegen = false;
	public float currentXP = 0f;
	public int currentLevel = 1;

	protected override void OnStart()
	{
		MaxHealth = Body;
		MaxStamina = Body;
		
		initialize();
	}

	public void gainXP ( float gainedXP )
	{
		currentXP += gainedXP;
		levelUp();
	}

	public void levelUp()
	{
		if ( currentXP >= 100f )
		{
			currentXP -= 100f;
			Strength += 1f;
			Sound.Play( levelUpSound, Transform.LocalPosition );
			currentLevel += 1;
			MaxHealth += Body;
			MaxStamina += Body;
			initialize();

			var log = Scene.GetAllComponents<BattleLog>().FirstOrDefault();
			log.AddTextLocal( $"✨ I've just hit Level {currentLevel}" );
		}
	}

	public void initialize()
	{
		Health = MaxHealth;
		Stamina = MaxStamina;
	}

	protected override void OnUpdate()
	{
		if ( _lastStamina >= 3f && Stamina != MaxStamina )
		{
			StaminaRegen = true;
			_lastStamina = 0f;
		}

		if ( StaminaRegen )
			StaminaDrain( -0.005f );
	}

	public void StaminaDrain( float staminaPercent )
	{
		if ( Stamina == MaxStamina )
			StaminaRegen = false;

		staminaPercent *= MaxStamina;

		Stamina = MathX.Clamp( Stamina - staminaPercent, 0f, MaxStamina );

		if ( Stamina > 0 )
			_lastStamina = 0f;
	}
}
