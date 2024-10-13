using Godot;
using System;
using System.Xml;

public partial class UImanager : Node
{
    [Export] CharacterSheet sheet;
    [Export] WeaponManager wm;
    
    ProgressBar hpbar, damagebar, staminabar;
    Label ammocounter;
    [Export] TimerBar reloadBar;

    ScaledTimer damageTimer;
    [Export] float damageDelay = 0.1f;

    public override void _Ready()
    {
        var cl = GetParent().GetParent().GetNode<CanvasLayer>("%CanvasLayer");
        hpbar = cl.GetNode<ProgressBar>("PlayerUI/Health Bar");
        staminabar = cl.GetNode<ProgressBar>("PlayerUI/Stamina Bar");
        damagebar = cl.GetNode<ProgressBar>("PlayerUI/Damage Bar");
        ammocounter = cl.GetNode<Label>("PlayerUI/Ammo Counter");
        ammocounter.Hide();

        sheet.HealthChanged += OnHealthChanged;
        sheet.StaminaChanged += OnStaminaChanged;
        
        wm.WeaponEquipped += OnWeaponEquipped;
        wm.WeaponUnequipped += OnWeaponUnequipped;
        wm.AmmoChanged += OnAmmoChanged;
        wm.ReloadStarted += OnReloadStarted;
        wm.ReloadFinished += OnReloadFinished;
        
        OnStaminaChanged(0);
        OnHealthChanged(0);
        
        targetHealth = (float)hpbar.Value;
        targetStamina = (float)staminabar.Value;

        hpbar.Value = (sheet.GetStatValue("CurrentHealth", false)/sheet.GetStatValue("Health", true)) * (float)hpbar.MaxValue;
        damagebar.Value = (sheet.GetStatValue("CurrentHealth", false)/sheet.GetStatValue("Health", true)) * (float)hpbar.MaxValue;
        staminabar.Value = (sheet.GetStatValue("CurrentStamina", false)/sheet.GetStatValue("Stamina", true))* (float)staminabar.MaxValue;

        reloadBar.HideAll();

        damageTimer = new ScaledTimer();
        AddChild(damageTimer);
    }


    private void OnWeaponUnequipped()
    {
        ammocounter.Hide();
    }


    private void OnWeaponEquipped()
    {
        if (wm.currWeapon.useCharge) ammocounter.Show();
        else ammocounter.Hide();
        OnAmmoChanged();
    }


    private void OnAmmoChanged()
    {
        ammocounter.Text = "AMMO:    " + wm.currWeaponSlotData.charge.ToString()  + "/" + wm.currWeapon.maxCharge.ToString();
    }


    private void OnReloadFinished()
    {
        reloadBar.HideAll();
        reloadBar.timer = null;
    }


    private void OnReloadStarted()
    {
        reloadBar.ShowAll();
        reloadBar.timer = wm.reloadTimer;
    }

    private float targetStamina;
    private void OnStaminaChanged(float change)
    {
        /*float baseSpeed = 20;
        stamLerpSpeed = Mathf.Clamp(Mathf.Abs(change), baseSpeed, 50);*/
    }
    
    private float targetHealth;
    private void OnHealthChanged(float change)
    {
        if (change < 0)
        {
            hpbar.Value = (sheet.GetStatValue("CurrentHealth", false)/sheet.GetStatValue("Health", true)) * (float)hpbar.MaxValue;
        }
        else
        {

        }
        
    }
    
    float hpLerpSpeed;
    float stamLerpSpeed = 30;
    [Export] float lerpSpeed = 10f;

    public override void _PhysicsProcess(double delta)
    {
        float D = sheet.game.Timescale * (float)delta;
        if (hpbar.Value < damagebar.Value)
        {
            damagebar.Value -= lerpSpeed * D;
            if (damagebar.Value < hpbar.Value) damagebar.Value = hpbar.Value;
        }
        
        float hpPercent = (sheet.GetStatValue("CurrentHealth", false)/sheet.GetStatValue("Health", true)) * (float)hpbar.MaxValue;
        if (hpbar.Value < hpPercent)
        {
            hpbar.Value += lerpSpeed * 3f * D;
            if (hpbar.Value > hpPercent) hpbar.Value = hpPercent;
            if (damagebar.Value < hpbar.Value) damagebar.Value = hpbar.Value;
        }

        float stamPercent = (sheet.GetStatValue("CurrentStamina", false)/sheet.GetStatValue("Stamina", true))* (float)staminabar.MaxValue;
        if (staminabar.Value > stamPercent)
        {
            staminabar.Value -= stamLerpSpeed * 3f * D;
            if (staminabar.Value < stamPercent) staminabar.Value = stamPercent;
        }
        else if (staminabar.Value < stamPercent)
        {
            staminabar.Value += stamLerpSpeed * 3f * D;
            if (staminabar.Value > stamPercent) staminabar.Value = stamPercent;
        }
    }

}
