using Yarn.Unity;

[System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.0.0.235")]
public partial class CustomGeneratedVariableStorage : GameStateManager, Yarn.Unity.IGeneratedVariableStorage {
    // Accessor for Bool $knows_fred
    /// <summary>
    /// do they know about fred?
    /// </summary>
    public bool KnowsFred {
        get => this.GetValueOrDefault<bool>("$knows_fred");
        set => this.SetValue<bool>("$knows_fred", value);
    }

    // Accessor for Number $gold_coins
    /// <summary>
    /// how many coins do you have?
    /// </summary>
    public float GoldCoins {
        get => this.GetValueOrDefault<float>("$gold_coins");
        set => this.SetValue<float>("$gold_coins", value);
    }

    // Accessor for String $player_name
    /// <summary>
    /// what is your name?
    /// </summary>
    public string PlayerName {
        get => this.GetValueOrDefault<string>("$player_name");
        set => this.SetValue<string>("$player_name", value);
    }

}
