using System.Windows;
using System.Windows.Controls;

namespace WindowApplication.UserControls;

public partial class TransferFormUserControl : UserControl
{
    public delegate void TransferFormHandler(decimal amount, string card_number);

    public event TransferFormHandler TransferEvent;
    
    public TransferFormUserControl(TransferFormHandler handler, string title)
    {
        InitializeComponent();
        TransferEvent += handler;
        TitleTextBlock.Text = title;
    }

    private void Entered(object sender, RoutedEventArgs e)
    {
        decimal amount;
        
        if (!decimal.TryParse(AmountTextBox.Text, out amount))
        {
            MessageBox.Show("Неправильний ввод.");
            return;
        }

        if (CardNumberTextBlock.Text.Length != 16)
        {
            MessageBox.Show("Неправильний номер картки.");
            return;
        }
        
        TransferEvent?.Invoke(amount, CardNumberTextBlock.Text);
    }

    private void CloseForm(object sender, RoutedEventArgs e)
    {
        Grid parent = (Grid)Parent;
        parent.Children.Remove(this);
    }
}