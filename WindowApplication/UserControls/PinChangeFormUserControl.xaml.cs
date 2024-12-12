using System.Windows;
using System.Windows.Controls;
using ClassLibrary.Data.Models;
using WindowApplication.Data;

namespace WindowApplication.UserControls;

public partial class PinChangeFormUserControl : UserControl
{
    public PinChangeFormUserControl()
    {
        InitializeComponent();
    }

    private void Entered(object sender, RoutedEventArgs e)
    {
        Account account = Items.main_window.context.Accounts.FirstOrDefault(a => a.CardNumber == CardNumberTextBox.Text);

        if (account == null)
        {
            MessageBox.Show("Недійсні облікові дані.");
            return;
        }

        if (NewPinCodeTextBox.Password != ConfirmPinCodeTextBox.Password)
        {
            MessageBox.Show("Неправильний пін-код.");
            return;
        }

        if (!BCrypt.Net.BCrypt.Verify(CurrentPinCodeTextBox.Password, account.PinCodeHash))
        {
            MessageBox.Show("Недійсні облікові дані.");
            return;
        }


        
        CloseForm(null, null);
    }

    private void CloseForm(object sender, RoutedEventArgs e)
    {
        Grid parent = (Grid)Parent;
        parent.Children.Remove(this);
    }
}