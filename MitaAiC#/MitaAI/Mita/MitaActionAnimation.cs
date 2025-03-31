using System.Text;
using System.Collections.Generic;
using UnityEngine;
using MitaAI;

public class MitaActionAnimation
{
    // Служит контейренром для анимации или интеракции
    enum ActionAnimationType
    {
        Animation,
        ObjectAnimation,
        PlayerAnimation
    }
    
    
    string Name;
    float begin_crossfade;
    float end_crossfade;
    float time;

    ActionAnimationType animationType;
    ObjectAnimationMita ObjectAnimationMita;

    public MitaActionAnimation(string name, float begin_crossfade, float end_crossfade, float time)
    {
        Name = name;
        this.begin_crossfade = begin_crossfade;
        this.end_crossfade = end_crossfade;
        this.time = time;
        this.animationType = ActionAnimationType.Animation;
    }
    public MitaActionAnimation(string name, float begin_crossfade, float end_crossfade, float time, ObjectAnimationMita objectAnimationMita)
    {
        Name = name;
        this.begin_crossfade = begin_crossfade;
        this.end_crossfade = end_crossfade;
        this.time = time;
        this.animationType = ActionAnimationType.ObjectAnimation;
        ObjectAnimationMita = objectAnimationMita;
    }
}
