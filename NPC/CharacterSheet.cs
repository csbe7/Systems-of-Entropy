using Godot;
using System;

public partial class CharacterSheet : CharacterBody3D
{
    public RandomNumberGenerator randomizer;


    [ExportCategory("Character Info")]
    //[Export] public CharacterStatus status;
    [Export] public string name;
    [Export] public Faction faction;

    
    [ExportCategory("Character Status")]
    public Game game;
    CharacterSheet sheet;
    public float localTimescale = 1;
    [Signal] public delegate void AttackedEventHandler(AttackInfo info);
    [Signal] public delegate void HealthChangedEventHandler(float change);
    [Signal] public delegate void StaminaChangedEventHandler(float change);
    
    CollisionObject3D body;

    [ExportCategory("Stats base value")]
    [Export] float health = 100;
    [Export] float stamina = 100;
    [Export] float staminaRegen = 5;
    [Export] float speed = 5;
    [Export] float dashSpeed = 15;
    [Export] public float dashTime = 0.1f;

    
    [ExportCategory("State stat modifiers")]
    [Export] public StatModifier sprintSpeedModifier;
    [Export] public StatModifier sneakSpeedModifier;
    [Export] public StatModifier strafeSpeedModifier;

    public int staminaRegenBlockers = 0;

    [ExportCategory("Action costs")]
    [Export] public float sprintStamPerSec = 20;
    [Export] public float sprintRecoveryTime = 2.5f;
    [Export] public float dashCost = 10;
    [Export] public float dashRecovery = 0.4f;
    [Export] public float dashStaminaRecoveryTime = 0.45f;


    public Godot.Collections.Dictionary stats = new Godot.Collections.Dictionary     //STATS
	{
		//STATUS STATS
		{"Health", new Stat(100)},
        {"CurrentHealth", new Stat(100)},
		{"Stamina", new Stat(100)},
		{"CurrentStamina", new Stat (100)},
        {"Tiredness", new Stat(100)},
        {"CurrentTiredness", new Stat(100)},
        {"Visibility", new Stat(0, 0, 100)},

        //MAIN STATS
        //MIND
        {"Logic", new Stat(1, 1, 20)},
        {"Knowledge", new Stat(1, 1, 20)},
        {"Persuasion", new Stat(1, 1, 20)},
        {"Emphaty", new Stat(1, 1, 20)},
        
        //SENSES
        {"Composure", new Stat(1, 1, 20)},
        {"Perception", new Stat(1, 1, 20)},
        {"Reflexes", new Stat(1, 1, 20)},
        {"Instincts", new Stat(1, 1, 20)},
        
        //BODY
		{"Strenght", new Stat(1, 1, 20)},
		{"Agility", new Stat(1, 1, 20)},
		{"Endurance", new Stat(1, 1, 20)},
        {"Dexterity", new Stat(1, 1, 20)},

        //SUB STATS
        {"Speed", new Stat()},
        {"Meele Damage", new Stat()},
        {"Stamina Regen", new Stat()},
        {"Dash Speed", new Stat()},
        {"Dash Recovery", new Stat()}

		
	};

    float invincibilityTime = 0;
    ScaledTimer invincibilityTimer;

    public override void _Ready()
    {
        randomizer = new RandomNumberGenerator();
        
        game = GetTree().Root.GetNode<Game>("Game");
        statusEffects = new Node();
        AddChild(statusEffects);
        
        if (!IsInstanceValid(body))
        {
            body = this;
            if (body == null) body = GetNodeOrNull<CollisionObject3D>("%Body");
        }
        invincibilityTimer = new ScaledTimer();
        invincibilityTimer.Timeout += EndInvincibility;
        AddTimer(invincibilityTimer);

        //Set stats
        SetStatValue("Health", health, 0);
        SetStatValue("CurrentHealth", health, 0);
        SetStatValue("Stamina", stamina, 0);
        SetStatValue("CurrentStamina", stamina, 0);
        SetStatValue("Stamina Regen", staminaRegen, 0);
        SetStatValue("Speed", speed, 0);
        SetStatValue("Dash Speed", dashSpeed, 0);
        SetStatValue("Dash Recovery", dashRecovery, 0);
    }

    public override void _Process(double delta)
    {
        float timescale = game.Timescale * localTimescale;
        float D = timescale * (float)delta;
        
        if (invincibilityTime > 0) invincibilityTime -= D;

        //REGEN STAMINA;
        if (staminaRegenBlockers == 0) ChangeStamina(GetStatValue("Stamina Regen",true) * D);
    }

    public void TakeAttack(AttackInfo attack)
    {
        EmitSignal(SignalName.Attacked, attack);
        ChangeHealth(-attack.damage);
    }

    public void ChangeHealth(float amount)
    {
        float hp =  ((Stat)stats["CurrentHealth"]).BaseValue; 
		hp = Mathf.Clamp(hp+amount, 0f, ((Stat)stats["Health"]).ModValue);
		SetStatValue("CurrentHealth", hp, 0);

        EmitSignal(SignalName.HealthChanged, amount);
        
    }

    public void ChangeStamina(float amount)
    {
        float stam = ((Stat)stats["CurrentStamina"]).BaseValue;
        stam = Mathf.Clamp(stam+amount, 0f, ((Stat)stats["Stamina"]).ModValue);
        SetStatValue("CurrentStamina", stam, 0);

        if (amount != 0)EmitSignal(SignalName.StaminaChanged, amount);
    }


    public void AddInvincibility(float time, int mode)
    {
        if (invincibilityTime < 0) invincibilityTime = 0;

        switch (mode)
        {
            case 0: //set if higher
            if (invincibilityTime < time) invincibilityTime = time;
            break;

            case 1: //simple add
            invincibilityTime += time;
            break;

            case 2: //simple set
            invincibilityTime = time;
            break;
        }
        
        if (invincibilityTime > 0)
        {
            StartInvincibility();
        }

    }   

    void StartInvincibility()
    {
        invincibilityTimer.Start(invincibilityTime);
        body.SetCollisionLayerValue(3, false);
    } 

    void EndInvincibility()
    {
        body.SetCollisionLayerValue(3, true);
    }

    

    //STAT MANIPULATION
    public float GetStatValue(string stat, bool mod = true) //true = ModValue  false = BaseValue
	{
		float value;
        if (mod)
		{
			value = ((Stat)stats[stat]).ModValue; 
            //((Stat)stats[stat]).hasBeenModified = true;
		}
		else
		{
			value = ((Stat)stats[stat]).BaseValue; 
		}

		return value;
	}
    public void SetStatValue(string stat, float value, int mode = 0)
    {
        switch (mode)
        {
            case 0: //SET
            ((Stat)stats[stat]).BaseValue = value;
            break;

            case 1: //ADD
            ((Stat)stats[stat]).BaseValue += value;
            break;
        }
    }
    public void ClampStatValue(string stat, float min, float max, int mode)
    {
        switch (mode)
        {
            case 0: //BASE
            ((Stat)stats[stat]).BaseValue = Mathf.Clamp(((Stat)stats[stat]).BaseValue, min, max);
            break;
        }
    }
    
    public void AddStatModifier(StatModifier mod, string targetStat)
	{
		((Stat)stats[targetStat]).AddModifier(mod);
	}
	public void RemoveStatModifier(StatModifier mod, string targetStat)
    {
        ((Stat)stats[targetStat]).RemoveModifier(mod);
    }
    
    Node statusEffects;
    public void AddStatusEffect(StatusEffect statusEffect)
	{
		statusEffect.target = sheet;
        statusEffects.AddChild(statusEffect);
        
		Godot.Collections.Array<StatusEffect> statusEffectList = GetStatusEffects();
		SortStatusEffects(statusEffectList, 0, statusEffectList.Count - 1);
	}


    public Godot.Collections.Array<StatusEffect> GetStatusEffects()
	{
        Godot.Collections.Array<StatusEffect> effects = new Godot.Collections.Array<StatusEffect>();

        foreach(Node child in statusEffects.GetChildren())
		{
			if (child is StatusEffect)
			{
				effects.Add((StatusEffect)child);
			}
		}

		return effects;
	}

    private void SortStatusEffects(Godot.Collections.Array<StatusEffect> array, int start, int end)
	{
		if (end <= start) return;
        
		int index, index2;
		StatusEffect temp;
		int i, j = start-1;

        for(i = start; i < end; i++)
		{
			if (array[i].priority < array[end].priority)
			{
				j++;
				temp = array[j];
                array[j] = array[i];
				array[i] = temp;
                
				index = array[i].GetIndex();
				index2 = array[j].GetIndex();
                
				statusEffects.MoveChild(array[i], index2);
				statusEffects.MoveChild(array[j], index);
			}
		}
		j++;
		temp = array[j];
		array[j] = array[end];
		array[end] = temp;
        
		index = array[i].GetIndex();
		index2 = array[j].GetIndex();

		statusEffects.MoveChild(array[i], index2);
		statusEffects.MoveChild(array[j], index);

		SortStatusEffects(array, start, j-1);
		SortStatusEffects(array, j+1, end);
	
	}



    public void SetTimescale(float timescale)
    {
        localTimescale = timescale;
        foreach(Node child in GetChildren())
        {
            if (child is ScaledTimer)
            {
                ((ScaledTimer)child).SetTimescale(localTimescale);
            }
        }
    }

    public void AddTimer(ScaledTimer timer)
    {
        timer.SetTimescale(localTimescale);
        AddChild(timer);
    }


    public Vector3 GetCenterPosition(WeaponManager wm = null)
    {
        if (IsInstanceValid(wm) && IsInstanceValid(wm.weaponTip))
        {
            return wm.weaponTip.GlobalPosition;
        }
        else
        {
            Shape3D shape = GetNode<CollisionShape3D>("%CollisionShape3D").Shape;
            if (shape is CapsuleShape3D capsule)
            {
                Vector3 pos = new Vector3(GlobalPosition.X, GlobalPosition.Y + capsule.Height/2, GlobalPosition.Z);
                return pos;
            }
            else if (shape is CylinderShape3D cylinder)
            {
                Vector3 pos = new Vector3(GlobalPosition.X, GlobalPosition.Y + cylinder.Height/2, GlobalPosition.Z);
                return pos;
            }
            else if (shape is SphereShape3D sphere)
            {
                Vector3 pos = new Vector3(GlobalPosition.X, GlobalPosition.Y + sphere.Radius, GlobalPosition.Z);
                return pos;
            }
        }
        return GlobalPosition;
    }

}
