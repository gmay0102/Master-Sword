using Sandbox;

public enum Rarity
{
	Common,
	Uncommon,
	Rare,
	Holyshit
}

public sealed class Modifier : Component
{

	[Property] Rarity rarityType { get; set; }
	[Property] UnitInfo unitInfo { get; set; }


	protected override void OnStart()
	{
		switch ( rarityType )
		{
			case Rarity.Common:
				break;
			case Rarity.Uncommon:
				unitInfo.CurrentLevel += 1;
				break;
			case Rarity.Rare:
				break;
			case Rarity.Holyshit:
				break;
			default:
				break;
		}
	}

	protected override void OnUpdate()
	{

	}
}
