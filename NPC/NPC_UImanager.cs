using Godot;
using System;

public partial class NPC_UImanager : Node
{
    [Export] CharacterSheet sheet;
    [Export] WeaponManager wm;
    [Export] NPC_AI ai;

    [Export] Control ui;

    ProgressBar hpbar, damagebar, staminabar, attackchargebar;
    TimerBar reloadBar;

    Label stateLabel;

    public override async void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SetProcess(true);
        SetPhysicsProcess(true);

        hpbar = ui.GetNode<ProgressBar>("%Health Bar");
        damagebar = ui.GetNode<ProgressBar>("%Damage Bar");
        staminabar = ui.GetNode<ProgressBar>("%Stamina Bar");
        attackchargebar = ui.GetNode<ProgressBar>("%Attack Charge Bar");

        stateLabel = ui.GetNode<Label>("State");

        reloadBar = ui.GetNode<TimerBar>("%Reload Bar");

        sheet.HealthChanged += OnHealthChanged;
        sheet.StaminaChanged += OnStaminaChanged;

        wm.ReloadStarted += OnReloadStarted;
        wm.ReloadFinished += OnReloadFinished;

        ai.StateChanged += OnAIStateChanged;
        ai.CombatStateChanged += OnAICombatStateChanged;

        OnStaminaChanged(0);
        OnHealthChanged(0);
        
        ui.Hide();
        
        attackchargebar.Hide();
        reloadBar.HideAll();
    }

    private void OnAICombatStateChanged()
    {
        stateLabel.Text = ai.combatState.ToString();
    }


    private void OnAIStateChanged()
    {
        if (ai.state == NPC_AI.State.combat)
        {
            ui.Show();
        }
        else
        {
            ui.Hide();
        }
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

    private void OnStaminaChanged(float change)
    {
        /*float baseSpeed = 20;
        stamLerpSpeed = Mathf.Clamp(Mathf.Abs(change), baseSpeed, 50);*/
    }
    
    private void OnHealthChanged(float change)
    {
        if (change < 0)
        {
            hpbar.Value = (sheet.GetStatValue("CurrentHealth", false)/sheet.GetStatValue("Health", true)) * (float)hpbar.MaxValue;
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

        if (ai.attackChargeTimer.countdown > 0 && ai.attackChargeTimer.countdown != NPC_AI.ATTACK_CHARGE_IDLE_VALUE)
        {
            attackchargebar.Show();
            float t = ai.wm.currWeapon.delayUseTime;
            float c = ai.attackChargeTimer.countdown;
            attackchargebar.Value = ((t - c)/t) * 100;
        }
        else
        {
            attackchargebar.Hide();
        }
    }
}
