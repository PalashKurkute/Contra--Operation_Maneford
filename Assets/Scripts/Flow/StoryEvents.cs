using System;

// A tiny message board for story moments. Gameplay code announces what
// just happened (StoryEvents.Raise("goal")) and anything interested,
// like overlay dialogue triggers, listens. Names are plain strings so
// triggers can be set up in the Inspector.
//
// Names used by the game:
//   kills_N         the Nth enemy of the stage is down (kills_1, kills_2...)
//   pack            a big pack of enemies was sent in
//   goal            the stage goal is done (the way forward opens)
//   boss_start, boss_missiles, boss_phase2, boss_charge, boss_defeated
//   chase_drones, general_60, general_30, general_20, general_down
public static class StoryEvents
{
    public static event Action<string> Raised;

    public static void Raise(string eventName)
    {
        if (Raised != null)
        {
            Raised(eventName);
        }
    }
}
