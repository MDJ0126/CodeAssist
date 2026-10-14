#include "FixtureActor.h"

float ABaseActor::GetHealth() const { return Health; }
UWorld* ABaseActor::GetWorld() { return nullptr; }
void AFixtureActor::Tick(float DeltaSeconds)
{
    int Count = 1;
    {
        int Count = 2;
        Health += static_cast<float>(Count);
    }
    Health += static_cast<float>(Count) + DeltaSeconds;
}
int AFixtureActor::Select(int Value) { return Value; }
double AFixtureActor::Select(double Value) { return Value; }
