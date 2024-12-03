using Godot;
using System;

public partial class SoundEmitter : AudioStreamPlayer3D
{
    [Export] CharacterSheet owner;
    [Export] public Godot.Collections.Array<Sound> sounds = new Godot.Collections.Array<Sound>();
    
    public void Play()
    {
        base.Play();
    }
    public void Play(int index)
    {
        Stream = sounds[index].stream;
        VolumeDb = sounds[index].volumeDb;
        base.Play();
    }
    
    public void Play(Sound sound, float pitchScale = 1, bool duplicate = true)
    {
        float oldPitch = PitchScale;
        PitchScale = pitchScale;
        Stream = sound.stream;
        VolumeDb = sound.volumeDb;
        base.Play();
        //PitchScale = oldPitch;
        EmitSound(sound, default(Vector3), duplicate);
    }


    async void EmitSound(Sound sound, Vector3 position = default, bool duplicate = true)
    {
        SphereShape3D hearRange = new SphereShape3D();
        hearRange.Radius = sound.maxHearingDistance;
        
        uint[] receptorLayer = {2};

        Vector3 pos;
        if (position != default(Vector3)) pos = position;
        else pos = GlobalPosition;
        
        var results = Game.Shapecast(this, hearRange, pos, Game.GetBitMask(receptorLayer));
        
        Sound s;
        if (duplicate) s = (Sound)sound.Duplicate();
        else s = sound;

        if (IsInstanceValid(owner) && !IsInstanceValid(s.emitter)) s.emitter = owner;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        foreach(var result in results)
        {
            if ((Node)result["collider"] is CharacterSheet sh)
            {
                AI ai = sh.GetNodeOrNull<AI>("%NPC AI");
                if (ai == null) ai = sh.GetNodeOrNull<AI>("%AI");
                if (ai == null) continue;
                
                ai.EmitSignal(AI.SignalName.SoundHeard, s, pos);
            }
        }
    }

}
