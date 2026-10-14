#include "FixtureActor.h"

int main()
{
    AFixtureActor Actor;
    const auto Health = Actor.GetHealth();
    Actor.Tick(0.016f);
    const auto Integer = Actor.Select(1);
    const auto Real = Actor.Select(1.0);
    // GetHealth is a comment, not a code reference.
    const char* Text = "GetHealth";
    return static_cast<int>(Health + Integer + Real) + static_cast<int>(Text[0]);
}
