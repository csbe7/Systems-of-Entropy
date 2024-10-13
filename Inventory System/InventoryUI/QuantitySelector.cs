using Godot;
using System;

public partial class QuantitySelector : Control
{
    public float min = 1;
    public float max = 999;
    
    LineEdit amountBox;
    HSlider amountSlider;
    Button cancelButton, confirmButton;
    bool dragging;
    
    [Signal] public delegate void QuantityConfirmedEventHandler(int amount);

    public override void _Ready()
    {
        amountBox = GetNode<LineEdit>("LineEdit");
        amountSlider = GetNode<HSlider>("HSlider");

        cancelButton = GetNode<Button>("%CancelButton");
        confirmButton = GetNode<Button>("%ConfirmButton");

        cancelButton.ButtonDown += Cancel;
        confirmButton.ButtonDown += Confirm;
        
        amountSlider.MaxValue = max;
        amountSlider.MinValue = min;

        amountBox.TextChanged += TextChanged;
        amountSlider.ValueChanged += SliderValueChanged;

        amountBox.FocusExited += boxDeselected;
    }


    public void TextChanged(string newText)
    {
        string overwrittenText = "";
        var caretColumn = amountBox.CaretColumn;
        
        int letter;
        foreach (int let in newText)
        {
            letter = let;
            if (letter >= '0' && letter <= '9')
            {
                overwrittenText += (char)letter;
            }
        }
        
        if (overwrittenText != "" && overwrittenText.ToInt() > max) 
        {
            overwrittenText = max.ToString();
            amountSlider.SetValueNoSignal(max);
        }
        else if (overwrittenText != "")
        {
            amountSlider.SetValueNoSignal(overwrittenText.ToInt());
        }
        else amountSlider.SetValueNoSignal(1);

        amountBox.Text = overwrittenText;
        amountBox.CaretColumn = caretColumn;   

       
    }

    public void boxDeselected()
    {
        if (amountBox.Text == "" || amountBox.Text == "0")
        {
            amountBox.Text = "1";
            
        }
    }

    
    public void SliderValueChanged(double newValue)
    {
        int intValue = (int)newValue;
        amountBox.Text = intValue.ToString();
    }
    

    public void Confirm()
    {
        EmitSignal(SignalName.QuantityConfirmed, amountBox.Text.ToInt());
        QueueFree();
    }

    public void Cancel()
    {
        QueueFree();
    }

}
