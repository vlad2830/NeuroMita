using System.Text;
using System.Collections.Generic;
using UnityEngine;
using MitaAI;

public class MitaActionAnimation
{
    // Служит контейнером для анимации или интеракции
    public enum ActionAnimationType
    {
        Animation,
        ObjectAnimation,
        PlayerAnimation
    }
    
    
    public string animName;
    public float begin_crossfade;
    public float end_crossfade;
    public float time;
    public float delay_after;
    public ActionAnimationType animationType;
    public ObjectAnimationMita ObjectAnimationMita;

    public MitaActionAnimation(string name, float begin_crossfade, float end_crossfade, float time, float delay_after = 0)
    {
        animName = name;
        this.begin_crossfade = begin_crossfade;
        this.end_crossfade = end_crossfade;
        this.time = time;
        this.animationType = ActionAnimationType.Animation;
        this.delay_after = delay_after;
    }
    public MitaActionAnimation(string name, float begin_crossfade, float end_crossfade, float time, ObjectAnimationMita objectAnimationMita, float delay_after = 0)
    {
        animName = name;
        this.begin_crossfade = begin_crossfade;
        this.end_crossfade = end_crossfade;
        this.time = time;
        this.animationType = ActionAnimationType.ObjectAnimation;
        ObjectAnimationMita = objectAnimationMita;
        this.delay_after = delay_after;
    }
}
