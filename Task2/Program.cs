using System;
using System.Collections.Generic;

namespace EnvironmentInteractionSystem
{
    public interface IEnvironmentObject
    {
        string Name { get; }
        void Accept(IEntity entity);
    }

    public interface IEnvironmentVisitor
    {
        void Visit(Chest chest);
        void Visit(Trap trap);
        void Visit(Campfire campfire);
    }


    public interface IEntity : IEnvironmentVisitor
    {
        string Name { get; }
        int HP { get; set; }
        void InteractWith(IEnvironmentObject obj);
    }


    public class Chest : IEnvironmentObject
    {
        public string Name { get; }
        public bool IsOpened { get; set; }
        public int GoldAmount { get; }

        public Chest(string name, int goldAmount = 50)
        {
            Name = name;
            GoldAmount = goldAmount;
        }

        public void Accept(IEntity entity) => entity.Visit(this);
    }

    public class Trap : IEnvironmentObject
    {
        public string Name { get; }
        public bool IsDisarmed { get; set; }
        public int Damage { get; }

        public Trap(string name, int damage = 25)
        {
            Name = name;
            Damage = damage;
        }

        public void Accept(IEntity entity) => entity.Visit(this);
    }

    public class Campfire : IEnvironmentObject
    {
        public string Name { get; }
        public bool IsLit { get; set; }

        public Campfire(string name, bool isLit = false)
        {
            Name = name;
            IsLit = isLit;
        }

        public void Accept(IEntity entity) => entity.Visit(this);
    }

    public abstract class EntityBase : IEntity
    {
        public string Name { get; }
        public int HP { get; set; }

        protected EntityBase(string name, int hp)
        {
            Name = name;
            HP = hp;
        }

        public void InteractWith(IEnvironmentObject obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            obj.Accept(this);
        }

        public abstract void Visit(Chest chest);
        public abstract void Visit(Trap trap);
        public abstract void Visit(Campfire campfire);
    }

    public class Player : EntityBase
    {
        public int Gold { get; private set; }

        public Player(string name, int hp) : base(name, hp) { }

        public override void Visit(Chest chest)
        {
            if (!chest.IsOpened)
            {
                chest.IsOpened = true;
                Gold += chest.GoldAmount;
                Console.WriteLine($"[Игрок {Name}] аккуратно взламывает '{chest.Name}' и забирает {chest.GoldAmount} золота (Всего золота: {Gold}).");
            }
            else
            {
                Console.WriteLine($"[Игрок {Name}] заглядывает в '{chest.Name}', но он уже пуст.");
            }
        }

        public override void Visit(Trap trap)
        {
            if (!trap.IsDisarmed)
            {
                trap.IsDisarmed = true;
                Console.WriteLine($"[Игрок {Name}] замечает механизм '{trap.Name}' и успешно обезвреживает ловушку!");
            }
            else
            {
                Console.WriteLine($"[Игрок {Name}] спокойно проходит мимо обезвреженной ловушки '{trap.Name}'.");
            }
        }

        public override void Visit(Campfire campfire)
        {
            if (!campfire.IsLit)
            {
                campfire.IsLit = true;
                Console.WriteLine($"[Игрок {Name}] разжигает '{campfire.Name}'. Становится тепло и уютно.");
            }
            else
            {
                HP += 10;
                Console.WriteLine($"[Игрок {Name}] отдыхает у горящего костра '{campfire.Name}' и восстанавливает 10 HP (HP: {HP}).");
            }
        }
    }

    public class Enemy : EntityBase
    {
        public Enemy(string name, int hp) : base(name, hp) { }

        public override void Visit(Chest chest)
        {
            Console.WriteLine($"[Враг {Name}] в ярости бьёт дубиной по '{chest.Name}', но замок слишком крепок.");
        }

        public override void Visit(Trap trap)
        {
            if (!trap.IsDisarmed)
            {
                HP = Math.Max(0, HP - trap.Damage);
                Console.WriteLine($"[Враг {Name}] наступает на '{trap.Name}' и получает {trap.Damage} урона! (Осталось HP: {HP}).");
            }
            else
            {
                Console.WriteLine($"[Враг {Name}] наступает на '{trap.Name}', но ловушка уже обезврежена.");
            }
        }

        public override void Visit(Campfire campfire)
        {
            if (campfire.IsLit)
            {
                campfire.IsLit = false;
                Console.WriteLine($"[Враг {Name}] затаптывает '{campfire.Name}', туша огонь!");
            }
            else
            {
                Console.WriteLine($"[Враг {Name}] пинает потухшие угли '{campfire.Name}'.");
            }
        }
    }

    public class Ally : EntityBase
    {
        public Ally(string name, int hp) : base(name, hp) { }

        public override void Visit(Chest chest)
        {
            Console.WriteLine($"[Союзник {Name}] проходит мимо '{chest.Name}', оставляя добычу лидеру отряда.");
        }

        public override void Visit(Trap trap)
        {
            if (!trap.IsDisarmed)
            {
                HP = Math.Max(0, HP - trap.Damage);
                Console.WriteLine($"[Союзник {Name}] не замечает '{trap.Name}' и подрывается на {trap.Damage} урона! (Осталось HP: {HP}).");
            }
            else
            {
                Console.WriteLine($"[Союзник {Name}] благодарит за обезвреженную ловушку '{trap.Name}'.");
            }
        }

        public override void Visit(Campfire campfire)
        {
            if (campfire.IsLit)
            {
                Console.WriteLine($"[Союзник {Name}] греет руки у костра '{campfire.Name}'.");
            }
            else
            {
                Console.WriteLine($"[Союзник {Name}] ждёт у '{campfire.Name}', пока кто-нибудь его разожжёт.");
            }
        }
    }

    class Program
    {
        static void Main()
        {
            var entities = new List<IEntity>
            {
                new Player("Геральт", 100),
                new Enemy("Гоблин-разведчик", 60),
                new Ally("Лютик", 80)
            };

            var environmentObjects = new List<IEnvironmentObject>
            {
                new Chest("Древний сундук", goldAmount: 150),
                new Trap("Капкан с шипами", damage: 20),
                new Campfire("Придорожный костёр", isLit: false)
            };

            Console.WriteLine("Симуляция взаимодействий с окружением\n");

            foreach (var entity in entities)
            {
                Console.WriteLine($" Действия сущности: {entity.Name} (HP: {entity.HP})");
                foreach (var envObj in environmentObjects)
                {
                    entity.InteractWith(envObj);
                }
                Console.WriteLine();
            }
        }
    }
}