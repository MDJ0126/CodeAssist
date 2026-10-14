#pragma once

// Standalone C++ fixture: names resemble UE APIs, but this is not a real UE build.
#define UCLASS(...)
#define UPROPERTY(...)
#define UFUNCTION(...)
#define GENERATED_BODY(...)

class UWorld { public: void SpawnActor() {} };
template<typename T> struct TObjectPtr { T* Value = nullptr; T* operator->() const { return Value; } };

class ABaseActor
{
public:
    float Health = 100.0f;
    float GetHealth() const;
    UWorld* GetWorld();
};

UCLASS()
class AFixtureActor : public ABaseActor
{
    GENERATED_BODY()
public:
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly)
    TObjectPtr<UWorld> World;
    UFUNCTION(BlueprintCallable)
    void Tick(float DeltaSeconds);
    int Select(int Value);
    double Select(double Value);
};
