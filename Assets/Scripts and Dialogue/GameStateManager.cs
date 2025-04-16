using System.Collections.Generic;
using UnityEngine;
using System;
using MPH;
using System.Linq;

public class VariableHashKeySource: IKeySource
{
    public VariableHashKeySource(string[] keys)
    {
        this.keys = keys;
    }

    private int currentKey = 0;
    private string[] keys;

    public uint NbKeys => (uint)keys.Length;

    public byte[] Read()
    {
        return System.Text.Encoding.UTF8.GetBytes(keys[currentKey++]);
    }
    public void Rewind()
    {
        currentKey = 0;
    }
}

public class GameStateManager : Yarn.Unity.VariableStorageBehaviour
{
    private List<IConvertible>[] gameState;

    private MinPerfectHash hashFunction;

    public Yarn.Unity.YarnProject project;

    void Start()
    {
        Initialise(Array.Empty<string>());
    }

    public void Initialise(string[] keys)
    {
        // if we don't have a project we will have to abort initialisation
        if (project == null)
        {
            Debug.LogError("Unable to initialise variable storage as there is no Yarn Project set");
            return;
        }

        // getting the initial values from the project
        // and merging that with the rest of the game keys
        var initialValues = project.InitialValues;
        List<string> yarnKeys = new();
        yarnKeys.AddRange(keys);
        yarnKeys.AddRange(initialValues.Keys);
        
        // generate a new hash function for the specific list of keys
        var keyHashGenerator = new VariableHashKeySource(yarnKeys.ToArray());
        hashFunction =  MinPerfectHash.Create(keyHashGenerator, 1);
        // now we make the values array of the size of the hash function
        gameState = new List<IConvertible>[hashFunction.N];

        // now we can add into the array the default yarn values
        foreach (var pair in initialValues)
        {
            uint index = hashFunction.IndexOf(pair.Key);
            gameState[index] = new List<IConvertible>()
            {
                pair.Value,
            };
        }
    }

    public void Rollback(string variableName)
    {
        if (hashFunction == null)
        {
            throw new InvalidOperationException();
        }

        var index = hashFunction.IndexOf(variableName);
        if (gameState[index] == null)
        {
            throw new ArgumentException();
        }

        var values = gameState[index];
        values.RemoveAt(values.Count - 1);
        gameState[index] = values;
    }
    public void RollbackAt(int index)
    {
        if (hashFunction == null)
        {
            throw new InvalidOperationException();
        }

        if (index < 0 || index >= gameState.Length)
        {
            throw new ArgumentException();
        }
        if (gameState[index] == null)
        {
            throw new ArgumentException();
        }

        var values = gameState[index];
        values.RemoveAt(values.Count - 1);
        gameState[index] = values;
    }
    
    public T[] ValuesAt<T>(int index)
    {
        if (hashFunction == null)
        {
            throw new InvalidOperationException();
        }

        if (index < 0 || index >= gameState.Length)
        {
            throw new ArgumentException();
        }
        var extant = gameState[index];
        T[] values = new T[extant.Count];
        for (int i = 0; i < extant.Count; i++)
        {
            values[i] = (T)extant[i];
        }
        return values;
    }
    public bool TryGetValues<T>(string variableName, out T[] result)
    {
        if (hashFunction == null)
        {
            result = default;
            return false;
        }

        var index = hashFunction.IndexOf(variableName);
        if (gameState[index] == null)
        {
            result = default;
            return false;
        }

        // adding all the elements to an array, letting you see the variable history
        var extant = gameState[index];
        T[] values = new T[extant.Count];
        for (int i = 0; i < extant.Count; i++)
        {
            values[i] = (T)extant[i];
        }

        result = values;
        return true;
    }

    public override bool TryGetValue<T>(string variableName, out T result)
    {
        // if we don't have a hash function we can't find the index
        if (hashFunction == null)
        {
            result = default;
            return false;
        }

        var index = hashFunction.IndexOf(variableName);
        var values = gameState[index];
        
        // if we have no value at that index we also can't return it
        if (values == null)
        {
            result = default;
            return false;
        }

        var value = values[^1];
        if (!typeof(T).IsAssignableFrom(value.GetType()))
        {
            result = default;
            return false;
        }

        result = (T)value;
        return true;
    }

    public void AddValueAt(int index, IConvertible value)
    {
        var values = gameState[index];
        values.Add(value);
        gameState[index] = values;
    }
    public void AddValue(string variableName, IConvertible value)
    {
        // if we don't have a hash function we can't find the index for where to add the new value
        if (hashFunction == null)
        {
            throw new InvalidOperationException();
        }

        var index = hashFunction.IndexOf(variableName);
        var values = gameState[index];

        // if we have no list at this index that means that the key is invalid
        if (values == null)
        {
            throw new ArgumentException();
        }

        // finally we can now add the new value to the list
        values.Add(value);
        gameState[index] = values;
    }

    public override void SetValue(string variableName, string stringValue)
    {
        AddValue(variableName, stringValue);
    }

    public override void SetValue(string variableName, float floatValue)
    {
        AddValue(variableName, floatValue);
    }

    public override void SetValue(string variableName, bool boolValue)
    {
        AddValue(variableName, boolValue);
    }

    public override void Clear()
    {
        gameState = null;
        hashFunction = null;
    }

    public override bool Contains(string variableName)
    {
        // if we don't have a hash function we can't see if we have a value for that key
        if (hashFunction == null)
        {
            throw new InvalidOperationException();
        }

        var index = hashFunction.IndexOf(variableName);
        var values = gameState[index];

        return values == null;
    }

    public override void SetAllVariables(Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools, bool clear = true)
    {
        if (hashFunction == null)
        {
            throw new InvalidOperationException();
        }

        foreach (var pair in floats)
        {
            AddValue(pair.Key, pair.Value);
        }
        foreach (var pair in bools)
        {
            AddValue(pair.Key, pair.Value);
        }
        foreach (var pair in strings)
        {
            AddValue(pair.Key, pair.Value);
        }
    }

    public override (Dictionary<string, float> FloatVariables, Dictionary<string, string> StringVariables, Dictionary<string, bool> BoolVariables) GetAllVariables()
    {
        if (hashFunction == null)
        {
            throw new InvalidOperationException();
        }
        if (project == null)
        {
            throw new InvalidOperationException();
        }

        Dictionary<string, float> allFloats = new();
        Dictionary<string, string> allStrings = new();
        Dictionary<string, bool> allBools = new();
        foreach (var key in project.InitialValues.Keys)
        {
            var index = hashFunction.IndexOf(key);
            var values = gameState[index];

            // if we have no list at this index that means that the key is invalid
            if (values == null)
            {
                continue;
            }

            var value = values[^1];
            if (value is bool v)
            {
                allBools[key] = v;
                continue;
            }
            if (value is float f)
            {
                allFloats[key] = f;
                continue;
            }
            if (value is string s)
            {
                allStrings[key] = s;
                continue;
            }
        }

        return (allFloats, allStrings, allBools);
    }
}