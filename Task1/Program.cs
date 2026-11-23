using System;
using System.Collections.Generic;
using System.Linq;

namespace RpgStatusSystem
{
    public interface IStatusEffect
    {
        string Name { get; }
        int RemainingTurns { get; }
        bool IsStackable { get; }
        bool IsExpired => RemainingTurns <= 0;

        void ApplyTurnEffect(Character target);

        void Tick();

        void OnReapply(IStatusEffect newEffect);
    }

    public abstract class StatusEffectBase : IStatusEffect
    {
        public string Name { get; }
        public int RemainingTurns { get; protected set; }
        public abstract bool IsStackable { get; }

        protected StatusEffectBase(string name, int duration)
        {
            if (duration <= 0)
                throw new ArgumentException("Длительность эффекта должна быть положительной.", nameof(duration));

            Name = name;
            RemainingTurns = duration;
        }

        public abstract void ApplyTurnEffect(Character target);

        public void Tick()
        {
            if (RemainingTurns > 0)
                RemainingTurns--;
        }

        public virtual void OnReapply(IStatusEffect newEffect)
        {
            if (IsStackable)
            {
                RemainingTurns += newEffect.RemainingTurns;
            }
            else
            {
                RemainingTurns = Math.Max(RemainingTurns, newEffect.RemainingTurns);
            }
        }

        public override string ToString() => $"{Name} (осталось ходов: {RemainingTurns})";
    }


    public class PoisonEffect : StatusEffectBase
    {
        private readonly int _damagePerTurn;
        public override bool IsStackable => true;

        public PoisonEffect(int damagePerTurn = 10, int duration = 3)
            : base("Отравление", duration)
        {
            _damagePerTurn = damagePerTurn;
        }

        public override void ApplyTurnEffect(Character target)
        {
            Console.WriteLine($"  [Эффект] {Name} наносит {_damagePerTurn} чистого урона (игнорируя броню).");
            target.TakeTrueDamage(_damagePerTurn);
        }
    }

    public class BurnEffect : StatusEffectBase
    {
        private readonly int _damagePerTurn;
        public override bool IsStackable => false;

        public BurnEffect(int damagePerTurn = 15, int duration = 2)
            : base("Горение", duration)
        {
            _damagePerTurn = damagePerTurn;
        }

        public override void ApplyTurnEffect(Character target)
        {
            Console.WriteLine($"  [Эффект] {Name} пытается нанести {_damagePerTurn} огненного урона.");
            target.TakeDamage(_damagePerTurn);
        }
    }

    public class RegenerationEffect : StatusEffectBase
    {
        private readonly int _healPerTurn;
        public override bool IsStackable => false;

        public RegenerationEffect(int healPerTurn = 12, int duration = 4)
            : base("Регенерация", duration)
        {
            _healPerTurn = healPerTurn;
        }

        public override void ApplyTurnEffect(Character target)
        {
            Console.WriteLine($"  [Эффект] {Name} восстанавливает {_healPerTurn} HP.");
            target.Heal(_healPerTurn);
        }
    }

    public class FreezeEffect : StatusEffectBase
    {
        public override bool IsStackable => false;

        public FreezeEffect(int duration = 1)
            : base("Заморозка", duration)
        {
        }

        public override void ApplyTurnEffect(Character target)
        {
            Console.WriteLine($"  [Эффект] {Name} сковывает персонажа! Он не может действовать в этот ход.");
            target.PreventAction();
        }
    }

    public class Character
    {
        private readonly List<IStatusEffect> _activeEffects = new();

        public string Name { get; }
        public int MaxHP { get; }
        public int HP { get; private set; }
        public int Armor { get; }
        public bool CanAct { get; private set; } = true;

        public IReadOnlyCollection<IStatusEffect> ActiveEffects => _activeEffects.AsReadOnly();

        public Character(string name, int maxHp, int armor)
        {
            Name = name;
            MaxHP = maxHp;
            HP = maxHp;
            Armor = armor;
        }

        public void TakeDamage(int rawDamage)
        {
            int mitigatedDamage = Math.Max(0, rawDamage - Armor);
            HP = Math.Max(0, HP - mitigatedDamage);
            Console.WriteLine($"    -> {Name} получает {mitigatedDamage} урона (заблокировано броней: {rawDamage - mitigatedDamage}). Текущее HP: {HP}/{MaxHP}");
        }

        public void TakeTrueDamage(int damage)
        {
            int actualDamage = Math.Max(0, damage);
            HP = Math.Max(0, HP - actualDamage);
            Console.WriteLine($"    -> {Name} получает {actualDamage} чистого урона. Текущее HP: {HP}/{MaxHP}");
        }

        public void Heal(int amount)
        {
            int oldHp = HP;
            HP = Math.Min(MaxHP, HP + Math.Max(0, amount));
            Console.WriteLine($"    -> {Name} исцеляется на {HP - oldHp} HP. Текущее HP: {HP}/{MaxHP}");
        }

        public void PreventAction()
        {
            CanAct = false;
        }

        public void ApplyEffect(IStatusEffect newEffect)
        {
            if (newEffect == null) throw new ArgumentNullException(nameof(newEffect));
            var existingEffect = _activeEffects.FirstOrDefault(e => e.GetType() == newEffect.GetType());

            if (existingEffect != null)
            {
                existingEffect.OnReapply(newEffect);
                string actionType = existingEffect.IsStackable ? "усилен (стак)" : "обновлён";
                Console.WriteLine($"[+] Эффект '{newEffect.Name}' {actionType} на {Name}. Новая длительность: {existingEffect.RemainingTurns} ход(а/ов).");
            }
            else
            {
                _activeEffects.Add(newEffect);
                Console.WriteLine($"[+] На {Name} наложен эффект: {newEffect}.");
            }
        }

        public void ProcessTurn()
        {
            Console.WriteLine($"\nНачало хода персонажа {Name}");
            CanAct = true;
            foreach (var effect in _activeEffects)
            {
                effect.ApplyTurnEffect(this);
            }

            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].Tick();
                if (_activeEffects[i].IsExpired)
                {
                    Console.WriteLine($"[-] Эффект '{_activeEffects[i].Name}' закончился и снят с {Name}.");
                    _activeEffects.RemoveAt(i);
                }
            }

            PrintStatus();
        }

        public void PrintStatus()
        {
            string effectsList = _activeEffects.Count > 0
                ? string.Join(", ", _activeEffects)
                : "нет";
            Console.WriteLine($"[Состояние] {Name}: HP {HP}/{MaxHP} | Броня: {Armor} | Может действовать: {(CanAct ? "Да" : "Нет")} | Эффекты: [{effectsList}]");
        }
    }

    class Program
    {
        static void Main()
        {
            var hero = new Character("Артур", maxHp: 100, armor: 5);
            hero.PrintStatus();
            Console.WriteLine();
            hero.ApplyEffect(new PoisonEffect(damagePerTurn: 8, duration: 2));
            hero.ApplyEffect(new BurnEffect(damagePerTurn: 12, duration: 2));
            hero.ApplyEffect(new FreezeEffect(duration: 1));
            hero.ApplyEffect(new PoisonEffect(damagePerTurn: 8, duration: 2));
            hero.ApplyEffect(new BurnEffect(damagePerTurn: 12, duration: 2));
            hero.ProcessTurn();
            Console.WriteLine();
            hero.ApplyEffect(new RegenerationEffect(healPerTurn: 10, duration: 3));
            hero.ProcessTurn();
            hero.ProcessTurn();
            hero.ProcessTurn();
        }
    }
}