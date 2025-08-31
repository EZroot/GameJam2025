using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CharacterSkills : MonoBehaviour
{
    public event Action<string, int> OnXpGained;
    public event Action<int> OnDamaged;
    public event Action<int> OnHealed;
    public event Action<int> OnLevelUp;
    public event Action OnDeath;

    [SerializeField] private SkillEntry[] m_skillEntryCollection;

    private Dictionary<SkillType, SkillEntry> m_skillDictionary;
    private CharacterContext m_characterContext;

    private void Awake()
    {
        m_characterContext = GetComponent<CharacterContext>();
        // build dictionary with keys from our skill entry list for fast access
        m_skillDictionary = new Dictionary<SkillType, SkillEntry>();
        for (int i = 0; i < m_skillEntryCollection.Length; i++)
        {
            SkillEntry skillEntry = m_skillEntryCollection[i];
            // If no level is set in inspector, default it to 1, else use inspector values
            if (skillEntry.Level == 0)
            {
                m_skillEntryCollection[i] = new SkillEntry(skillEntry.Name, skillEntry.SkillType); 
            }
            else
            {
                m_skillEntryCollection[i] = new SkillEntry(skillEntry); 
            }

            if (!m_skillDictionary.ContainsKey(skillEntry.SkillType))
            {
                // Initialize our skill entry data properly so that theres no shared reference
                // And so the constructor properly builds the data
                m_skillDictionary.Add(m_skillEntryCollection[i].SkillType, m_skillEntryCollection[i]);
            }
            else
            {
                Debug.LogError($"Duplicate skill entry found: {skillEntry.SkillType}. Ignoring duplicate.");
            }
        }
    }

    public SkillEntry GetSkill(SkillType skillType)
    {
        if (m_skillDictionary.TryGetValue(skillType, out var skillEntry))
        {
            return skillEntry;
        }
        Debug.LogError($"Skill type {skillType} not found.");
        return null;
    }

    public void AddXp(SkillType skillType, int xp)
    {
        if (m_skillDictionary.TryGetValue(skillType, out var skillEntry))
        {
            skillEntry.AddXp(xp);
            OnXpGained?.Invoke(skillEntry.Name, xp);
        }
        else
        {
            Debug.LogError($"Skill type {skillType} not found. Cannot add XP.");
        }
    }

    public void AddDamage(int damage)
    {
        if (m_skillDictionary.TryGetValue(SkillType.Health, out var healthSkill))
        {
            healthSkill.RemoveCurrentWorkValue(damage);
            OnDamaged?.Invoke(damage);

            var speechLine = UnityEngine.Random.Range(0, 10);
            switch (speechLine)
            {
                case 0:
                    m_characterContext.CharacterSpeech.Say("Ouch!");
                    break;
                case 1:
                    m_characterContext.CharacterSpeech.Say("Oof!");
                    break;
                case 2:
                    m_characterContext.CharacterSpeech.Say("Ahgh!");
                    break;
                case 3:
                    m_characterContext.CharacterSpeech.Say("Humphf!");
                    break;
                case 4:
                    m_characterContext.CharacterSpeech.Say("Oaugh");
                    break;
            }

            if (healthSkill.GetCurrentWorkValue() <= 0)
            {
                OnDeath?.Invoke();
            }
        }
    }

    public void AddHealth(int health)
    {
        if (m_skillDictionary.TryGetValue(SkillType.Health, out var healthSkill))
        {
            healthSkill.AddCurrentWorkValue(health);
            OnHealed?.Invoke(health);
        }
    }
}

[System.Serializable]
public enum SkillType
{
    Combat,
    Health,
    Toughness,
    Speed
}

[System.Serializable]
public class SkillEntry
{
    public string Name;
    public SkillType SkillType;
    public int Level;
    public int CurrentXp;
    public int XpToNextLevel;
    public float SkillBuffMultiplier;

    private int MaxWorkValue; // e.g., damage for Combat, max health for Health, etc.
    private int CurrentWorkValue; // e.g., current health for Health

    public SkillEntry(SkillEntry entry)
    {
        Name = entry.Name;
        Level = entry.Level;
        CurrentXp = 0;
        XpToNextLevel = CalculateXpRequiredNextLevel(Level); // Initial XP requirement for level 2
        SkillType = entry.SkillType;
        MaxWorkValue = CalculateWorkValue(Level);
        CurrentWorkValue = MaxWorkValue;
        SkillBuffMultiplier = 1f;
    }

    public SkillEntry(string name, SkillType type)
    {
        Name = name;
        Level = 1;
        CurrentXp = 0;
        XpToNextLevel = 100; // Initial XP requirement for level 2
        SkillType = type;
        MaxWorkValue = CalculateWorkValue(Level);
        CurrentWorkValue = MaxWorkValue;
        SkillBuffMultiplier = 1f;
    }

    public int GetCurrentWorkValue()
    {
        return Mathf.CeilToInt(CurrentWorkValue * SkillBuffMultiplier);
    }
    public int GetMaxWorkValue()
    {
        return Mathf.CeilToInt(MaxWorkValue * SkillBuffMultiplier);
    }

    public void SetSkillBuff(float multiplier = 1)
    {
        SkillBuffMultiplier = multiplier;
    }

    public void AddCurrentWorkValue(int amount)
    {
        CurrentWorkValue += amount;
        if (CurrentWorkValue < 0) CurrentWorkValue = 0;
        if (CurrentWorkValue > MaxWorkValue) CurrentWorkValue = MaxWorkValue;
    }

    public void RemoveCurrentWorkValue(int amount)
    {
        AddCurrentWorkValue(-amount);
    }

    public void AddXp(int xp)
    {
        CurrentXp += xp;
        while (CurrentXp >= XpToNextLevel)
        {
            CurrentXp -= XpToNextLevel;
            Level++;
            XpToNextLevel = CalculateXpRequiredNextLevel(Level);
            MaxWorkValue = CalculateWorkValue(Level);
            CurrentWorkValue = MaxWorkValue; // Restore work value on level up
            Debug.Log($"{Name} leveled up to {Level}!");
        }
    }

    private int CalculateXpRequiredNextLevel(int lvl)
    {
        return (int)(Mathf.Floor(lvl + 300 * Mathf.Pow(2, lvl / 7f)) / 4);
    }

    private int CalculateWorkValue(int lvl)
    {
        switch (SkillType)
        {
            case SkillType.Combat:
                return 10 + (lvl*3 - 1); // Example: base damage + 2 per level
            case SkillType.Health:
                return 100 + (lvl - 1) * 10; // Example: base health + 10 per level
            case SkillType.Toughness:
                return 5 + (lvl - 1); // Example: base armor + 1 per level
            case SkillType.Speed:
                return 2 + (lvl - 1); // Example: base speed + 1 per level
            default:
                return 0;
        }
    }
}

