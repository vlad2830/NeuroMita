using System.Text;
using System.Collections.Generic;
using UnityEngine;
using MitaAI;

public class MitaActionAnimation
{
    // Служит контейренром для анимации или интеракции
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
    public ActionAnimationType animationType;
    public ObjectAnimationMita ObjectAnimationMita;

    public MitaActionAnimation(string name, float begin_crossfade, float end_crossfade, float time)
    {
        animName = name;
        this.begin_crossfade = begin_crossfade;
        this.end_crossfade = end_crossfade;
        this.time = time;
        this.animationType = ActionAnimationType.Animation;
    }
    public MitaActionAnimation(string name, float begin_crossfade, float end_crossfade, float time, ObjectAnimationMita objectAnimationMita)
    {
        animName = name;
        this.begin_crossfade = begin_crossfade;
        this.end_crossfade = end_crossfade;
        this.time = time;
        this.animationType = ActionAnimationType.ObjectAnimation;
        ObjectAnimationMita = objectAnimationMita;
    }
}
