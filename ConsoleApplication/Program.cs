using System.Text;
using ClassLibrary.Data.Models;
using ClassLibrary.Data.TDOs;
using CLassLibrary.Data.Models;
using static ClassLibrary.Data.Models.Account;

namespace ConsoleApplication;

class Program
{

    static void Main(string[] args)

    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        AccountStateHandler SendMessage = (sender, e) =>
        {
            Console.WriteLine(e.Message);
        };

        WithdrawnEvent += SendMessage;
        AddedEvent += SendMessage;

        ViewBalanceEvent += (sender, e) =>
        {
            Console.WriteLine("Баланс: " + ((Account)sender).CurrentSum);
        };

        AuthorizedEvent += (sender, e) =>
        {
            Console.Write("Введіть номер картки: ");
            string card_number = Console.ReadLine();

            Console.Write("Введіть пін-код: ");
            string pin_code = Console.ReadLine();

            Account account = context.Accounts.FirstOrDefault(a => a.CardNumber == card_number);

            if (account == null)
            {
                Console.WriteLine("Недійсні облікові дані.");
                Console.ReadLine();
                return null;
            }
            if (!BCrypt.Net.BCrypt.Verify(pin_code, account.PinCodeHash))
            {
                Console.WriteLine("Недійсні облікові дані.");
                Console.ReadLine();
                Authorize();
                return null;
            }

            CurrentAccount = account;
            ATMMenu();

            return account;
        };

        TransferEvent += (sender, e) =>
        {
            if (e.Sum > CurrentAccount.CurrentSum)
            {
                Console.WriteLine("Не вистачає коштів.");
                return;
            }

            Account receiver = context.Accounts.FirstOrDefault(a => a.CardNumber == e.Receiver);

            if (receiver == null)
            {
                Console.WriteLine("Неправильний номер картки.");
                return;
            }

            CurrentAccount.CurrentSum -= (decimal)e.Sum;
            receiver.CurrentSum += (decimal)e.Sum;
            context.SaveChanges();

            Console.WriteLine($"Ви успішно перевели {e.Sum} на {e.Receiver}");
        };

        CreatedEvent += (sender, e) =>
        {
            AccountTDO account_tdo = (AccountTDO)e.Object;

            Account new_account = new Account()
            {
                FirstName = account_tdo.FirstName,
                LastName = account_tdo.LastName,
                CompanyId = account_tdo.CompanyId,
                PinCodeHash = BCrypt.Net.BCrypt.HashPassword(account_tdo.PinCode),
                CardNumber = GenerateCardNumber(),
            };

            context.Accounts.Add(new_account);
            context.SaveChanges();

            Console.WriteLine("Номер картки: " + new_account.CardNumber);

            return new_account;
        };

        MainMenu();
    }

    public static ActionEvent MainMenu = () => CreateOptions
    (
        new string[] { "Банкомати", "Банки", "Вийти" },
        new ActionEvent[] { ATMs, Banks, () => { Environment.Exit(0); } }
    );



    public static decimal GetAmount()
    {
        decimal amount;
        bool result;

        do
        {
            Console.Write("Сума: ");
            result = decimal.TryParse(Console.ReadLine(), out amount);

            if (!result)
            {
                Console.WriteLine("Неправильний ввод.");
            }
        } while (!result);

        return amount;
    }

    public static ActionEvent BanksMenu = () => CreateOptions
    (
        new string[] { "Створити аккаунт", "Відкрити банкомат", "Назад" },
        new ActionEvent[] {() =>
        {
            AccountTDO account_tdo = new AccountTDO();

            Console.Write("Введіть ваше ім'я: ");
            account_tdo.FirstName = Console.ReadLine();

            Console.Write("Введіть вашу фамілію: ");
            account_tdo.LastName = Console.ReadLine();

            Console.Write("Введіть ваш пін-код: ");
            string pin_code = Console.ReadLine();

            Console.Write("Підтвердіть ваш пін-код:");
            string confirm_pin_code = Console.ReadLine();

            if(pin_code != confirm_pin_code)
            {
                Console.WriteLine("Неправильний пін-код.");
                Console.ReadLine();
                BanksMenu();
            }

            if(pin_code.Length != 4)
            {
                Console.WriteLine("Пін-код має бути хоча б 5 цифр.");
                Console.ReadLine();
                BanksMenu();
            }

            account_tdo.PinCode = pin_code;
            account_tdo.CompanyId = Bank.CurrentBank.CompanyId;

            Create(account_tdo);
            Console.ReadKey();
            BanksMenu();
        }, () =>
        {
            ATM.CurrentATM = new()
            {
                CompanyId = Bank.CurrentBank.CompanyId,
                Balance = 100000,
            };

            AccountLogin();
        }, MainMenu}
    );

    public static ActionEvent ATMMenu = () => CreateOptions
    (
        new string[] { "Мій баланс", "Зняття готівки", "Депозит", "Перевод", "Назад" },
        new ActionEvent[] {() =>
        {
            CurrentAccount.ViewBalance();
            Console.ReadKey();
            Console.Clear();
            ATMMenu();
        }, () =>
        {

            CurrentAccount.Withdraw(GetAmount(), 0);
            Console.ReadKey();
            ATMMenu();
        }, () =>
        {
            CurrentAccount.Put(GetAmount());
            Console.ReadKey();
            ATMMenu();
        },
        () =>
        {
            decimal amount = GetAmount();

            Console.Write("Номер картки: ");
            string card_number = Console.ReadLine();

            CurrentAccount.Transfer(amount, card_number);

            Console.ReadLine();
            ATMMenu();
        },
        () =>
        {
            MainMenu();
        }}
    );

    public static void AccountLogin()
    {
        Authorize();
    }

    public delegate void ActionEvent();

    public static void CreateOptions(string[] Options, ActionEvent[] Actions)
    {
        if (Options.Length != Actions.Length) return;

        Console.Clear();

        while (true)
        {
            for (int index = 0; index < Options.Length; index++)
            {
                Console.WriteLine($"{index + 1}. {Options[index]}");
            }

            int option = Console.ReadKey().KeyChar - 48;
            Console.WriteLine();

            try
            {
                Console.Clear();
                Actions[option - 1]?.Invoke();
                break;
            }
            catch
            {
                Console.Clear();
                Console.WriteLine("Неправильний вибір. Спробуйте ще раз.");
            }
        }
    }


    public static void ATMs()
    {
        List<ATM> ATMs = context.ATMs.ToList();

        for (int index = 0; index < ATMs.Count; index++)
        {
            Company company = context.Companies.FirstOrDefault(c => c.Id == ATMs[index].CompanyId);

            Console.WriteLine($"{index + 1} | {company.Name} | {ATMs[index].Address}");
        }

        while (true)
        {
            try
            {
                int option = int.Parse(Console.ReadLine());
                ATM.CurrentATM = ATMs[option - 1];

                Console.Clear();
                Authorize();
            }
            catch
            {
                Console.WriteLine("Неправильний айді.");
            }
        }
    }

    public static void Banks()
    {
        List<Bank> Banks = context.Banks.ToList();

        for (int index = 0; index < Banks.Count; index++)
        {
            Company company = context.Companies.FirstOrDefault(c => c.Id == Banks[index].CompanyId);

            Console.WriteLine($"{index + 1} | {company.Name} | {Banks[index].Address}");
        }

        while (true)
        {
            try
            {
                int option = int.Parse(Console.ReadLine());
                Bank.CurrentBank = Banks[option - 1];

                Console.Clear();
                BanksMenu();
            }
            catch
            {
                Console.WriteLine("Неправильний айді.");
            }
        }
    }
}
