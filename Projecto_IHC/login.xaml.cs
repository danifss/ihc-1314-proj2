using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;



namespace Projecto_IHC
{
	/// <summary>
	/// Interaction logic for login.xaml
	/// </summary>
	public partial class login : Window
	{
		// tcp: 193.136.175.33\SQLSERVER2012,8293



		SqlConnection myConnection = new SqlConnection("user id=p5g4;" + "password=pretobranco;" + "server=tcp: 193.136.175.33\\SQLSERVER2012,8293;" +  "database=p5g4 ");

		public login()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			string tipo;

			aviso_login.Visibility = Visibility.Hidden;
			login_button.IsEnabled = false;

			if (combobox_login.SelectedIndex == 0)
				tipo = "A";
			else
				tipo = "F";

			try
			{
				string sql1 = "select Numero, Nome, Username, Password, Tipo, Codigo_loja from PROJECTO.Funcionario WHERE Username='" + textbox_username.Text + "' AND Tipo='" + tipo + "'";
				SqlCommand query1 = new SqlCommand(sql1, myConnection);

				myConnection.Open();

				SqlDataReader myReader;
				myReader = query1.ExecuteReader();

				if (myReader.Read())
				{
					if ((myReader["Password"].Equals(password.Password)) && (myReader["Username"].ToString().Equals(textbox_username.Text)))
					{
						// *********** ID do funcionario online *************
						App.Current.Properties["NUMERO_FUNC"] = myReader["Numero"].ToString();
						App.Current.Properties["USERNAME"] = myReader["Nome"].ToString();
            App.Current.Properties["USER_TYPE"] = tipo.ToString();
						App.Current.Properties["NUM_LOJA"] = myReader["Codigo_loja"].ToString();

						MainWindow janela = new MainWindow();
            this.Close();
						janela.ShowDialog();
					}
				}
				else
				{
					aviso_login.Content = "Login Errado!";
					aviso_login.Visibility = Visibility.Visible;
					textbox_username.Text = "Username";
					password.Password = "aaaaaaaa";
				}
			}
			catch (SqlException ex)
			{
				aviso_login.Content = ("Falha na ligação a Internet! -> " + ex.Message);
				aviso_login.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();
			}
		}

		private void combobox_login_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (combobox_login.SelectedIndex == 0)
			{
				Admin_imagem.Visibility = Visibility.Visible;
				func_imagem.Visibility = Visibility.Hidden;
			}
			else
			{

				Admin_imagem.Visibility = Visibility.Hidden;

				func_imagem.Visibility = Visibility.Visible;

			}

		}





		private void Button_Click_1(object sender, RoutedEventArgs e)
		{

			//Application.Current.Exit();
			this.Close();

		}





		private void password_GotFocus(object sender, RoutedEventArgs e)
		{

			password.Clear();

			login_button.IsEnabled = true;

		}



		private void textbox_username_GotFocus(object sender, RoutedEventArgs e)
		{

			textbox_username.Clear();

			login_button.IsEnabled = true;

		}

		private void password_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Button_Click(sender, e);
			}
		}
	}
}
