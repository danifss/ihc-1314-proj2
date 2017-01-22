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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using System.Data;
using System.Data.SqlClient;

namespace Projecto_IHC
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// 
	public partial class MainWindow : Window
	{
		DispatcherTimer timer;

		char lojaUltimoBotao = 'N';
		char clienteUltimoBotao = 'N';
		char funcionarioUltimoBotao = 'N';
		char produtoUltimoBotao = 'N';

		SqlConnection myConnection = new SqlConnection("user id=p5g4;" + "password=pretobranco;" + "server=tcp: 193.136.175.33\\SQLSERVER2012,8293;" + "database=p5g4");

		public MainWindow()
		{
			this.InitializeComponent();
			// Actualizar Hora e data
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(1.0);
			timer.Start();
			timer.Tick += new EventHandler(delegate(object s, EventArgs a) { hora.Content = "" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second; data.Content = DateTime.Now.ToString("dd/MM/yyyy"); });

			if (App.Current.Properties["USERNAME"] == null || App.Current.Properties["USER_TYPE"] == null || App.Current.Properties["NUM_LOJA"] == null)
			{
				App.Current.Properties["NUMERO_FUNC"] = "1";
				App.Current.Properties["USERNAME"] = "Utilizador de Teste";
				App.Current.Properties["USER_TYPE"] = "A";
				App.Current.Properties["NUM_LOJA"] = "1";
			}
			lblUser_nome.Content = App.Current.Properties["USERNAME"].ToString();
			lblUser_tipo.Content = ((App.Current.Properties["USER_TYPE"].Equals("A")) ? "Administrador" : "Funcionário");

			if (App.Current.Properties["USER_TYPE"].ToString() != ("A"))
				tabitem_loja.IsEnabled = false;
		}

		private int getNewID(string tabela, string campo)
		{
			int newNumber = 0; // numero maximo por omissao
			try
			{
				SqlCommand query = new SqlCommand("SELECT " + campo + " FROM PROJECTO." + tabela + " ORDER BY " + campo + " ASC", myConnection);
				SqlDataReader myReader;
				myConnection.Open();
				myReader = query.ExecuteReader();

				while (myReader.Read())
				{
					int n = int.Parse(myReader[campo].ToString().Trim());
					if (n > newNumber)
						newNumber = n; // novo maximo
				}
			}
			catch (SqlException ex)
			{
				lblLojaResult.Visibility = Visibility.Visible;
				lblLojaResult.Content = "Erro a obter novo ID! " + ex.Message;
				return -1;
			}
			finally
			{
				myConnection.Close();
			}
			return newNumber + 1;
		}
		private void Button_Click_3(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Button_Click_4(object sender, RoutedEventArgs e)
		{
			login janela = new login();
			this.Close();
			janela.ShowDialog();
		}

		/***************************************** TAB LOJA *****************************************/
		private void cboLoja_search_GotFocus(object sender, RoutedEventArgs e) // carregar combobox
		{
			try
			{
				myConnection.Close(); // tem de ter isto porque chega aqui com a conexao aberta e da erro. nao sei porque...
				cboLoja_search.Items.Clear();
				string sql = "SELECT Nome FROM PROJECTO.Loja ORDER BY Codigo ASC";
				SqlCommand query = new SqlCommand(sql, myConnection);
				SqlDataReader myReader;
				myConnection.Open();
				myReader = query.ExecuteReader();

				while (myReader.Read())
				{
					string item = myReader["Nome"].ToString();
					cboLoja_search.Items.Add(item);
				}
			}
			catch (Exception ex)
			{
				aviso_loja.Content = "Erro a carregar as Lojas! " + ex.Message;
				aviso_loja.Visibility = Visibility.Visible;
				//cboLoja_search.Focus();
			}
			finally
			{
				myConnection.Close();
			}
		}

		private void cboLoja_search_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			cboLoja_tipo.SelectedIndex = 2; // nome
			btnLoja_search.IsEnabled = true;
		}

		private void btnLoja_search_Click(object sender, RoutedEventArgs e)
		{
			txtLoja_endereco.IsEnabled = false;
			txtLoja_nome.IsEnabled = false;
			txtLoja_nif.IsEnabled = false;
			chkboxLoja_ativa.IsEnabled = false;
			aviso_loja.Visibility = Visibility.Hidden;
			lblLojaResult.Visibility = Visibility.Hidden;
			try
			{
				SqlDataReader myReader = null;
				SqlCommand query1;

				if (cboLoja_tipo.SelectedIndex == 0) // by Codigo
					query1 = new SqlCommand("SELECT * FROM PROJECTO.Loja WHERE Codigo=" + cboLoja_search.Text, myConnection);
				else if (cboLoja_tipo.SelectedIndex == 1) // by NIF
					query1 = new SqlCommand("SELECT * FROM PROJECTO.Loja WHERE NIF=" + cboLoja_search.Text, myConnection);
				else // by Nome
					query1 = new SqlCommand("SELECT * FROM PROJECTO.Loja WHERE Nome='" + cboLoja_search.Text + "'", myConnection);

				myConnection.Open();
				myReader = query1.ExecuteReader();
				if (myReader.Read())
				{
					btnLoja_eliminar.IsEnabled = true;
					btnLoja_alterar.IsEnabled = true;

					txtLoja_num.Text = myReader["Codigo"].ToString();
					txtLoja_nome.Text = myReader["Nome"].ToString();
					txtLoja_endereco.Text = myReader["Endereco"].ToString();
					txtLoja_nif.Text = myReader["NIF"].ToString();
					if (myReader["Ativo"].Equals("1"))
						chkboxLoja_ativa.IsChecked = true;
					else
						chkboxLoja_ativa.IsChecked = false;
				}
				else
					throw new Exception("Loja inexistente!");
			}
			catch (Exception ex)
			{
				aviso_loja.Content = "ERRO! " + ex.Message;
				aviso_loja.Visibility = Visibility.Visible;
				cboLoja_search.Focus();
			}
			finally
			{
				myConnection.Close();
				cboLoja_search.SelectedValue = "";
				cboLoja_search.Text = "";
				cboLoja_search.Items.Clear();
			}
		}

		// A - adicionar
		// T - alterar
		private void btnLoja_confirmar_Click(object sender, RoutedEventArgs e)
		{
			if (lojaUltimoBotao == 'A') // adicionar nova loja
			{
				int novoNumero = getNewID("Loja", "Codigo");
				try
				{
					if (novoNumero == -1)
						throw new Exception("Nao foi possível obter novo ID.");

					string sql = "INSERT INTO PROJECTO.Loja (Nome, Endereco, NIF, Codigo, Ativo) VALUES ('" + txtLoja_nome.Text + "','" + txtLoja_endereco.Text + "','" + txtLoja_nif.Text + "','" + novoNumero + "','" + ((chkboxLoja_ativa.IsChecked == true) ? "1" : "0") + "');";
					SqlCommand query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível criar nova Loja.");

					// mostrar mensagem e chamar funcao limpar
					lblLojaResult.Content = "Loja adicionada com sucesso.";
					lblLojaResult.Visibility = Visibility.Visible;
				}
				catch (Exception ex)
				{
					cboLoja_search.Text = "";
					txtLoja_nif.Clear();
					btnLoja_search.IsEnabled = false;
					txtLoja_num.Clear();
					txtLoja_endereco.Clear();
					txtLoja_nome.Clear();
					txtLoja_nif.IsEnabled = false;
					txtLoja_nome.IsEnabled = false;
					txtLoja_endereco.IsEnabled = false;
					cboLoja_tipo.SelectedIndex = 0;
					aviso_loja.Content = "Erro! " + ex.Message;
					aviso_loja.Visibility = Visibility.Visible;
				}
				finally
				{
					myConnection.Close();
				}
			}
			else if (lojaUltimoBotao == 'T') // alterar loja existente
			{
				try
				{
					string sql = "UPDATE PROJECTO.Loja SET Nome='" + txtLoja_nome.Text + "', Endereco='" + txtLoja_endereco.Text + "', NIF='" + txtLoja_nif.Text + "', Ativo=" + ((chkboxLoja_ativa.IsChecked == true) ? "1" : "0") + " WHERE Codigo=" + txtLoja_num.Text;
					SqlCommand query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					int result = query.ExecuteNonQuery();

					// mostrar mensagem e chamar funcao limpar
					lblLojaResult.Content = "Loja alterada com sucesso.";
					lblLojaResult.Visibility = Visibility.Visible;
				}
				catch (SqlException ex)
				{
					cboLoja_search.Text = "";
					txtLoja_nif.Clear();
					btnLoja_search.IsEnabled = false;
					txtLoja_num.Clear();
					txtLoja_endereco.Clear();
					txtLoja_nome.Clear();
					txtLoja_nif.IsEnabled = false;
					txtLoja_nome.IsEnabled = false;
					txtLoja_endereco.IsEnabled = false;
					cboLoja_tipo.SelectedIndex = 0;
					aviso_loja.Content = "Erro! " + ex.Message;
					aviso_loja.Visibility = Visibility.Visible;
				}
				finally
				{
					myConnection.Close();
				}
			}
			cboLoja_search.Text = "";
			cboLoja_search.Items.Clear();
			txtLoja_nif.Clear();
			btnLoja_search.IsEnabled = false;
			txtLoja_num.Clear();
			txtLoja_endereco.Clear();
			txtLoja_nome.Clear();
			txtLoja_nif.IsEnabled = false;
			txtLoja_nome.IsEnabled = false;
			txtLoja_endereco.IsEnabled = false;
			btnLoja_alterar.IsEnabled = false;
			btnLoja_confirmar.IsEnabled = false;
			btnLoja_eliminar.IsEnabled = false;
			btnLoja_adicionar.IsEnabled = true;
			cboLoja_tipo.SelectedIndex = 0;
			aviso_loja.Visibility = Visibility.Hidden;
			cboLoja_search.Focus();
		}

		private void btnLoja_limpar_Click(object sender, RoutedEventArgs e)
		{
			cboLoja_search.Text = "";
			cboLoja_search.Items.Clear();
			txtLoja_nif.Clear();
			btnLoja_search.IsEnabled = false;
			txtLoja_num.Clear();
			txtLoja_endereco.Clear();
			txtLoja_nome.Clear();
			txtLoja_nif.IsEnabled = false;
			txtLoja_nome.IsEnabled = false;
			txtLoja_endereco.IsEnabled = false;
			btnLoja_adicionar.IsEnabled = true;
			btnLoja_alterar.IsEnabled = false;
			btnLoja_confirmar.IsEnabled = false;
			btnLoja_eliminar.IsEnabled = false;
			lblLojaResult.Visibility = Visibility.Hidden;
			cboLoja_tipo.SelectedIndex = 0;
			aviso_loja.Visibility = Visibility.Hidden;
			cboLoja_search.Focus();
		}

		private void btnLoja_alterar_Click(object sender, RoutedEventArgs e)
		{
			if (txtLoja_num.Text != "")
			{
				txtLoja_nome.IsEnabled = true;
				txtLoja_endereco.IsEnabled = true;
				txtLoja_nif.IsEnabled = true;
				btnLoja_adicionar.IsEnabled = false;
				btnLoja_eliminar.IsEnabled = false;
				btnLoja_confirmar.IsEnabled = true;
				btnLoja_alterar.IsEnabled = false;
				chkboxLoja_ativa.IsEnabled = true;
				txtLoja_nome.Focus();
			}
			lojaUltimoBotao = 'T';
		}

		private void btnLoja_adicionar_Click(object sender, RoutedEventArgs e)
		{
			txtLoja_endereco.IsEnabled = true;
			txtLoja_nome.IsEnabled = true;
			txtLoja_nif.IsEnabled = true;
			btnLoja_confirmar.IsEnabled = false;
			btnLoja_eliminar.IsEnabled = false;
			btnLoja_alterar.IsEnabled = false;
			txtLoja_nome.Focus();
			aviso_loja.Visibility = Visibility.Hidden;
			cboLoja_search.Text = "";
			txtLoja_nif.Clear();
			btnLoja_search.IsEnabled = false;
			txtLoja_num.Clear();
			btnLoja_adicionar.IsEnabled = false;
			txtLoja_endereco.Clear();
			txtLoja_nome.Clear();
			chkboxLoja_ativa.IsEnabled = true;
			cboLoja_tipo.SelectedIndex = 0;
			lojaUltimoBotao = 'A';
			txtLoja_num.Text = getNewID("Loja", "Codigo").ToString();
		}

		private void btnLoja_eliminar_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult res = MessageBox.Show("Tem a certeza que deseja eliminar?", "Confirmar a eliminação", MessageBoxButton.YesNo);
			if (res == MessageBoxResult.Yes) // Sim
			{
				try
				{
					myConnection.Open();
					SqlCommand query = new SqlCommand("UPDATE PROJECTO.Loja SET Ativo='0' WHERE Codigo='" + txtLoja_num.Text + "'", myConnection);
					int result = query.ExecuteNonQuery();
					if (result == 1) // se afetou um registo esta correto
					{
						lblLojaResult.Content = "A Loja " + txtLoja_nome.Text + " foi apagada com sucesso!";
						lblLojaResult.Visibility = Visibility.Visible;
						txtLoja_num.Clear();
						txtLoja_nome.Clear();
						txtLoja_endereco.Clear();
						txtLoja_nif.Clear();
					}
				}
				catch (SqlException ex)
				{
					aviso_loja.Content = "Erro! " + ex.Message;
				}
				finally
				{
					myConnection.Close();
				}
			}
		}

		private void txtLoja_nif_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtLoja_nome.Text != "" & txtLoja_endereco.Text != "" & txtLoja_nif.Text != "" & txtLoja_nif.IsEnabled == true)
				btnLoja_confirmar.IsEnabled = true;
			else
				btnLoja_confirmar.IsEnabled = false;

		}

		private void txtLoja_endereco_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtLoja_nome.Text != "" & txtLoja_endereco.Text != "" & txtLoja_nif.Text != "" & txtLoja_nif.IsEnabled == true)
				btnLoja_confirmar.IsEnabled = true;
			else
				btnLoja_confirmar.IsEnabled = false;

		}

		private void txtLoja_nome_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtLoja_nome.Text != "" & txtLoja_endereco.Text != "" & txtLoja_nif.Text != "" & txtLoja_nif.IsEnabled == true)
				btnLoja_confirmar.IsEnabled = true;
			else
				btnLoja_confirmar.IsEnabled = false;

		}

		private void cboLoja_tipo_DropDownClosed(object sender, EventArgs e)
		{
			cboLoja_search.Items.Clear();
			cboLoja_search.Focus();
		}

		private void cboLoja_search_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) // pesquisar com a tecla Enter
				btnLoja_search_Click(sender, e);
		}
		/***************************************** FIM TAB CLIENTE *****************************************/

		private void Button_Click_7(object sender, RoutedEventArgs e)
		{
			tabcontrol.Visibility = Visibility.Hidden;
			grid_opcoes.Visibility = Visibility.Visible;
			gridFunc_campos.Visibility = Visibility.Hidden;
			retanguloAjuda.Visibility = Visibility.Hidden;
			retangulo.Visibility = Visibility.Visible;
			gridFuncionario.Visibility = Visibility.Hidden;

		}

		private void Button_Click_9(object sender, RoutedEventArgs e)
		{
			tabcontrol.Visibility = Visibility.Visible;
			grid_opcoes.Visibility = Visibility.Hidden;
			gridFunc_campos.Visibility = Visibility.Hidden;
			retangulo.Visibility = Visibility.Hidden;
			retanguloAjuda.Visibility = Visibility.Hidden;
			gridFuncionario.Visibility = Visibility.Hidden;
		}

		private void funcionarios_Click(object sender, RoutedEventArgs e)
		{
			tabcontrol.Visibility = Visibility.Hidden;
			gridFunc_campos.Visibility = Visibility.Visible;
			grid_opcoes.Visibility = Visibility.Hidden;
			retangulo.Visibility = Visibility.Visible;
			retanguloAjuda.Visibility = Visibility.Hidden;
			gridFuncionario.Visibility = Visibility.Visible;
		}

		private void ajuda_Click(object sender, RoutedEventArgs e)
		{
			tabcontrol.Visibility = Visibility.Hidden;
			gridFunc_campos.Visibility = Visibility.Hidden;
			grid_opcoes.Visibility = Visibility.Hidden;
			retangulo.Visibility = Visibility.Visible;
			retanguloAjuda.Visibility = Visibility.Visible;
			gridFuncionario.Visibility = Visibility.Hidden;

		}

		private void calculadora_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("calc");
		}

		private void button_confirmar_comprar_Click(object sender, RoutedEventArgs e)
		{

		}

		private void txtComprarClienteNum_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtComprarClienteNum.Text != "" && textbox_referencia_comprar.Text != "" && ((combobox_pagamento_comprar.SelectedIndex == 1 && conta_destino_comprar.Text != "" && conta_origem_comprar.Text != "") || (combobox_pagamento_comprar.SelectedIndex == 0)))
				button_confirmar_comprar.IsEnabled = true;
			else
				button_confirmar_comprar.IsEnabled = false;
		}

		private void textbox_referencia_comprar_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtComprarClienteNum.Text != "" && textbox_referencia_comprar.Text != "" && ((combobox_pagamento_comprar.SelectedIndex == 1 && conta_destino_comprar.Text != "" && conta_origem_comprar.Text != "") || (combobox_pagamento_comprar.SelectedIndex == 0)))
				button_confirmar_comprar.IsEnabled = true;
			else
				button_confirmar_comprar.IsEnabled = false;
		}

		private void combobox_pagamento_comprar_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (combobox_pagamento_comprar.SelectedIndex == 0)
			{
				conta_destino_comprar.IsEnabled = false;
				conta_origem_comprar.IsEnabled = false;
			}
			else
			{
				conta_destino_comprar.IsEnabled = true;
				conta_origem_comprar.IsEnabled = true;
			}
		}

		private void combobox_pagamento_comprar_DropDownClosed(object sender, EventArgs e)
		{
			if (combobox_pagamento_comprar.SelectedIndex == 0)
			{
				conta_destino_comprar.IsEnabled = false;
				conta_origem_comprar.IsEnabled = false;
				if (textbox_referencia_comprar.Text != "" && txtComprarClienteNum.Text != "")
					button_confirmar_comprar.IsEnabled = true;
			}
			else
			{
				button_confirmar_comprar.IsEnabled = false;
				conta_destino_comprar.IsEnabled = true;
				conta_origem_comprar.IsEnabled = true;
				conta_destino_comprar.Focus();
			}
		}

		private void ComboBox_DropDownClosed(object sender, EventArgs e)
		{
			txtComprarClienteSearch.Focus();
		}

		private void ComboBox_DropDownClosed_1(object sender, EventArgs e)
		{
			ref_input_comprar.Focus();
		}

		private void cod_imput_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtComprarClienteSearch.Text != "")
				pesquisa_cliente_comprar.IsEnabled = true;
			else
				pesquisa_cliente_comprar.IsEnabled = false;
		}

		private void ref_input_comprar_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (ref_input_comprar.Text != "")
				pesquisa_produto_comprar.IsEnabled = true;
			else
				pesquisa_produto_comprar.IsEnabled = false;

		}

		private void conta_destino_comprar_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtComprarClienteNum.Text != "" && textbox_referencia_comprar.Text != "" && ((combobox_pagamento_comprar.SelectedIndex == 1 && conta_destino_comprar.Text != "" && conta_origem_comprar.Text != "") || (combobox_pagamento_comprar.SelectedIndex == 0)))
				button_confirmar_comprar.IsEnabled = true;
			else
				button_confirmar_comprar.IsEnabled = false;
		}

		private void conta_origem_comprar_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtComprarClienteNum.Text != "" && textbox_referencia_comprar.Text != "" && ((combobox_pagamento_comprar.SelectedIndex == 1 && conta_destino_comprar.Text != "" && conta_origem_comprar.Text != "") || (combobox_pagamento_comprar.SelectedIndex == 0)))
				button_confirmar_comprar.IsEnabled = true;
			else
				button_confirmar_comprar.IsEnabled = false;
		}

		private void ComboBox_DropDownClosed_2(object sender, EventArgs e)
		{
			textbox_cliente_vender.Focus();
		}

		private void ComboBox_DropDownClosed_3(object sender, EventArgs e)
		{
			textbox_produto_vender.Focus();
		}

		private void textbox_cliente_vender_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_cliente_vender.Text != "")
				botao_pesquisar_cliente_vender.IsEnabled = true;
			else
				botao_pesquisar_cliente_vender.IsEnabled = false;
		}

		private void textbox_produto_vender_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_produto_vender.Text != "")
				botao_pesquisar_produto_vender.IsEnabled = true;
			else
				botao_pesquisar_produto_vender.IsEnabled = false;
		}

		private void textbox_num_cliente_vender_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_num_cliente_vender.Text != "" && textbox_ref_vender.Text != "" && ((combobox_pagamento_vender.SelectedIndex == 1 && conta_destino_vender.Text != "" && conta_origem_vender.Text != "") || (combobox_pagamento_vender.SelectedIndex == 0)))
				botao_confirmar_vender.IsEnabled = true;
			else
				botao_confirmar_vender.IsEnabled = false;
		}

		private void textbox_ref_vender_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_num_cliente_vender.Text != "" && textbox_ref_vender.Text != "" && ((combobox_pagamento_vender.SelectedIndex == 1 && conta_destino_vender.Text != "" && conta_origem_vender.Text != "") || (combobox_pagamento_vender.SelectedIndex == 0)))
				botao_confirmar_vender.IsEnabled = true;
			else
				botao_confirmar_vender.IsEnabled = false;
		}

		private void conta_destino_vender_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_num_cliente_vender.Text != "" && textbox_ref_vender.Text != "" && ((combobox_pagamento_vender.SelectedIndex == 1 && conta_destino_vender.Text != "" && conta_origem_vender.Text != "") || (combobox_pagamento_vender.SelectedIndex == 0)))
				botao_confirmar_vender.IsEnabled = true;
			else
				botao_confirmar_vender.IsEnabled = false;
		}

		private void conta_origem_vender_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_num_cliente_vender.Text != "" && textbox_ref_vender.Text != "" && ((combobox_pagamento_vender.SelectedIndex == 1 && conta_destino_vender.Text != "" && conta_origem_vender.Text != "") || (combobox_pagamento_vender.SelectedIndex == 0)))
				botao_confirmar_vender.IsEnabled = true;
			else
				botao_confirmar_vender.IsEnabled = false;
		}

		private void ComboBox_DropDownClosed_4(object sender, EventArgs e)
		{
			if (combobox_pagamento_vender.SelectedIndex == 0)
			{
				conta_destino_vender.IsEnabled = false;
				conta_origem_vender.IsEnabled = false;
				if (textbox_ref_vender.Text != "" && textbox_num_cliente_vender.Text != "")
					botao_confirmar_vender.IsEnabled = true;
			}
			else
			{
				botao_confirmar_vender.IsEnabled = false;
				conta_destino_vender.IsEnabled = true;
				conta_origem_vender.IsEnabled = true;
				conta_destino_vender.Focus();
			}

		}

		private void ComboBox_DropDownClosed_5(object sender, EventArgs e)
		{
			textbox_cliente_reserva.Focus();
		}

		private void ComboBox_DropDownClosed_6(object sender, EventArgs e)
		{
			textbox_produto_reserva.Focus();
		}

		private void textbox_cliente_reserva_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_cliente_reserva.Text != "")
				pesquisa_cliente_reserva.IsEnabled = true;
			else
				pesquisa_cliente_reserva.IsEnabled = false;
		}

		private void textbox_produto_reserva_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_produto_reserva.Text != "")
				pesquisa_produto_reserva.IsEnabled = true;
			else
				pesquisa_produto_reserva.IsEnabled = false;
		}

		private void textbox_num_cliente_reserva_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_num_cliente_reserva.Text != "" && textbox_ref_reserva.Text != "")
				botao_confirmar_reserva.IsEnabled = true;
			else
				botao_confirmar_reserva.IsEnabled = false;
		}

		private void textbox_ref_reserva_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (textbox_num_cliente_reserva.Text != "" && textbox_ref_reserva.Text != "")
				botao_confirmar_reserva.IsEnabled = true;
			else
				botao_confirmar_reserva.IsEnabled = false;
		}

		/***************************************** TAB COMPRAR *****************************************/
		private void pesquisa_cliente_comprar_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SqlDataReader myReader;
				SqlCommand query1;

				// NIF SELECIONADO
				if (comboboxComprarCliente.SelectedIndex == 0)
					query1 = new SqlCommand("select * from PROJECTO.Cliente WHERE NIF=" + txtComprarClienteSearch.Text, myConnection);
				// NUM CLIENTE
				else if (comboboxComprarCliente.SelectedIndex == 1)
					query1 = new SqlCommand("select * from PROJECTO.Cliente WHERE Numero=" + txtComprarClienteSearch.Text, myConnection);
				// NUM CARTAO
				else
					query1 = new SqlCommand("select * from PROJECTO.Cliente where Num_cartão=" + txtComprarClienteSearch.Text, myConnection);

				myConnection.Open();
				myReader = query1.ExecuteReader();
				if (myReader.Read())
				{
					lblComprarClienteAviso.Visibility = Visibility.Hidden;
					txtComprarClienteNum.Text = myReader["Numero"].ToString();
					txtComprarClienteNome.Text = myReader["Nome"].ToString();
					txtComprarClienteEmail.Text = myReader["Email"].ToString();
					txtComprarClienteEndereco.Text = myReader["Endereco"].ToString();
					txtComprarClienteTele.Text = myReader["Telefone"].ToString();
					txtComprarClienteNIF.Text = myReader["NIF"].ToString();
				}
				else // o cliente nao existe
				{
					lblComprarClienteAviso.Visibility = Visibility.Visible;
					lblComprarClienteAviso.Content = "Cliente não existe!";
					txtComprarClienteSearch.Focus();
				}
			}
			catch (Exception ex)
			{
				lblComprarClienteAviso.Visibility = Visibility.Visible;
				lblComprarClienteAviso.Content = "ERRO!! " + ex.Message;
				txtComprarClienteSearch.Focus();
			}
			finally
			{
				myConnection.Close();
			}
		}

		private void TabItem_GotFocus(object sender, RoutedEventArgs e)
		{
			try
			{
				myConnection.Open();

				SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM PROJECTO.Compra", myConnection);

				DataSet ds = new DataSet();

				da.Fill(ds);

				datagridHist.DataContext = ds;

			}
			catch (SqlException ex)
			{
				Console.WriteLine("------------------\n" + ex.Message);
			}
			finally
			{
				myConnection.Close();
			}
		}
		/***************************************** FIM TAB COMPRAR *****************************************/

		/***************************************** TAB CLIENTE *****************************************/
		private void btnCliente_search_Click(object sender, RoutedEventArgs e)
		{
			btnCliente_confirmar.IsEnabled = false;
			btnCliente_adicionar.IsEnabled = false;
			gridCliente.IsEnabled = false;
			btnCliente_limpar.IsEnabled = true;
			lblCliente_aviso.Visibility = Visibility.Hidden;
			try
			{
				string searchType = "";
				if (cboCliente_tipo.SelectedIndex == 0) // by num cliente
					searchType = "Numero";
				else if (cboCliente_tipo.SelectedIndex == 1) // by num cartao
					searchType = "Num_cartao";
				else // by nif
					searchType = "NIF";

				string sql = "SELECT *, PROJECTO.Cartao.Num_cartao AS CartaoNumCartao FROM PROJECTO.Cliente INNER JOIN PROJECTO.Cartao ON PROJECTO.Cliente.Num_cartao=PROJECTO.Cartao.Num_cartao WHERE PROJECTO.Cliente." + searchType + "='" + txtCliente_search.Text + "'";

				SqlCommand query = new SqlCommand(sql, myConnection);
				myConnection.Open();
				SqlDataReader myReader = query.ExecuteReader();
				if (myReader.Read())
				{
					btnCliente_alterar.IsEnabled = true;
					if (App.Current.Properties["USER_TYPE"].ToString().Equals("A")) // se for administrador
						btnCliente_eliminar.IsEnabled = true;
					txtCliente_search.Clear();
					txtCliente_search.Focus();

					txtCliente_codigo.Text = myReader["Numero"].ToString();
					txtCliente_nome.Text = myReader["Nome"].ToString();
					txtCliente_email.Text = myReader["Email"].ToString();
					dateCliente_dNasc.Text = myReader["Data_nasc"].ToString();
					txtCliente_endereco.Text = myReader["Endereco"].ToString();
					txtCliente_tele.Text = myReader["Telefone"].ToString();
					txtCliente_nif.Text = myReader["NIF"].ToString();
					if (myReader["Ativo"].ToString().Equals("True") == true)
						chkboxCliente_ativo.IsChecked = true;
					else
						chkboxCliente_ativo.IsChecked = false;
					txtCliente_numCartao.Text = myReader["CartaoNumCartao"].ToString();
					txtCliente_pontos.Text = myReader["Pontos"].ToString();
				}
				else
					throw new Exception("Cliente Inexistente!");
			}
			catch (Exception ex)
			{
				lblCliente_aviso.Content = "Erro! " + ex.Message;
				btnCliente_limpar_Click(sender, e);
				lblCliente_aviso.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();
			}
		}

		private void btnCliente_confirmar_Click(object sender, RoutedEventArgs e)
		{
			btnCliente_confirmar.IsEnabled = false;
			btnCliente_adicionar.IsEnabled = false;
			btnCliente_limpar.IsEnabled = true;
			lblCliente_aviso.Visibility = Visibility.Hidden;
			try
			{
				string sql;
				SqlCommand query;
				if (clienteUltimoBotao == 'A') // Adicionar um cliente
				{
					if (txtCliente_numCartao.Text.Equals("-1"))
						throw new Exception("Nao foi possível obter novo número de cartão.");
					if (txtCliente_codigo.Text.Equals("-1"))
						throw new Exception("Nao foi possível obter novo número de cliente.");

					// criar um novo cartao antes de inserir um cliente
					sql = "INSERT INTO PROJECTO.Cartao (Num_cartao, Pontos) VALUES (" + txtCliente_numCartao.Text + ", 0)";
					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível criar novo cartão.");
					myConnection.Close();

					// criar novo cliente e associar cartao
					sql = "INSERT INTO PROJECTO.Cliente (Nome,Endereco,Email,Telefone,Numero,NIF,Data_nasc,Num_cartao,Ativo) VALUES ('" + txtCliente_nome.Text + "','" + txtCliente_endereco.Text + "','" + txtCliente_email.Text + "'," + txtCliente_tele.Text + "," + txtCliente_codigo.Text + "," + txtCliente_nif.Text + ",'" + dateCliente_dNasc.Text + "'," + txtCliente_numCartao.Text + "," + ((chkboxCliente_ativo.IsChecked == true) ? "1" : "0") + ")";

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível criar novo cliente.");

					lblCliente_aviso.Content = "Cliente adicionado com sucesso";
					lblCliente_aviso.Foreground = new SolidColorBrush(Colors.DarkGreen);
					lblCliente_aviso.Visibility = Visibility.Visible;
				}
				else if (clienteUltimoBotao == 'T') // Editar um cliente
				{
					sql = "UPDATE PROJECTO.Cliente SET Nome='" + txtCliente_nome.Text + "', Endereco='" + txtCliente_endereco.Text + "', Email='" + txtCliente_email.Text + "', Telefone=" + txtCliente_tele.Text + ", NIF=" + txtCliente_nif.Text + ", Data_nasc='" + dateCliente_dNasc.Text.Trim() + "', Ativo=" + ((chkboxCliente_ativo.IsChecked == true) ? "1" : "0") + " WHERE Numero=" + txtCliente_codigo.Text;

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível criar novo cliente.");
					myConnection.Close();

					// atualizar os pontos do cartao
					sql = "UPDATE PROJECTO.Cartao SET Pontos=" + txtCliente_pontos.Text + " WHERE Num_cartao=" + txtCliente_numCartao.Text;
					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível atualizar os pontos do cartão.");

					lblCliente_aviso.Content = "Cliente alterado com sucesso";
					lblCliente_aviso.Foreground = new SolidColorBrush(Colors.DarkGreen);
					lblCliente_aviso.Visibility = Visibility.Visible;
				}
			}
			catch (Exception ex)
			{
				lblCliente_aviso.Content = "Erro! " + ex.Message;
				btnCliente_limpar_Click(sender, e);
				lblCliente_aviso.Foreground = new SolidColorBrush(Colors.DarkRed);
				lblCliente_aviso.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();
				btnCliente_limpar_Click(sender, e);
				lblCliente_aviso.Visibility = Visibility.Visible;
			}
		}

		private void btnCliente_eliminar_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult res = MessageBox.Show("Tem a certeza que deseja eliminar?", "Confirmar a eliminação", MessageBoxButton.YesNo);
			if (res == MessageBoxResult.Yes) // Sim
			{
				try
				{
					string sql = "UPDATE PROJECTO.Cliente SET Ativo=0 WHERE Numero=" + txtCliente_codigo.Text;
					SqlCommand query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível eliminar cartão.");

					lblCliente_aviso.Content = "Cliente eliminado com sucesso";
					lblCliente_aviso.Foreground = new SolidColorBrush(Colors.DarkGreen);
					lblCliente_aviso.Visibility = Visibility.Visible;
				}
				catch (Exception ex)
				{
					lblCliente_aviso.Content = "Erro! " + ex.Message;
					btnCliente_limpar_Click(sender, e);
					lblCliente_aviso.Foreground = new SolidColorBrush(Colors.DarkRed);
					lblCliente_aviso.Visibility = Visibility.Visible;
				}
				finally
				{
					myConnection.Close();
					btnCliente_limpar_Click(sender, e);
					lblCliente_aviso.Visibility = Visibility.Visible;
				}
			}
		}

		private void btnCliente_alterar_Click(object sender, RoutedEventArgs e)
		{
			gridCliente.IsEnabled = true;
			txtCliente_nome.IsEnabled = true;
			txtCliente_tele.IsEnabled = true;
			txtCliente_email.IsEnabled = true;
			txtCliente_endereco.IsEnabled = true;
			dateCliente_dNasc.IsEnabled = true;
			txtCliente_nif.IsEnabled = true;
			txtCliente_nome.Focus();
			if (App.Current.Properties["USER_TYPE"].Equals("A")) // se for admin pode alterar os pontos e se esta ativo
			{
				chkboxCliente_ativo.IsEnabled = true;
				txtCliente_pontos.IsEnabled = true;
				btnCliente_eliminar.IsEnabled = true;
			}
			btnCliente_alterar.IsEnabled = false;
			btnCliente_confirmar.IsEnabled = true;
			btnCliente_adicionar.IsEnabled = false;
			clienteUltimoBotao = 'T';
		}

		private void btnCliente_adicionar_Click(object sender, RoutedEventArgs e)
		{
			gridCliente.IsEnabled = true;
			btnCliente_adicionar.IsEnabled = false;
			btnCliente_alterar.IsEnabled = false;
			btnCliente_confirmar.IsEnabled = true;
			btnCliente_eliminar.IsEnabled = false;
			btnCliente_limpar.IsEnabled = true;
			txtCliente_nome.IsEnabled = true;
			txtCliente_tele.IsEnabled = true;
			txtCliente_email.IsEnabled = true;
			txtCliente_endereco.IsEnabled = true;
			dateCliente_dNasc.IsEnabled = true;
			txtCliente_nif.IsEnabled = true;
			lblCliente_aviso.Visibility = Visibility.Hidden;
			txtCliente_codigo.Text = getNewID("Cliente", "Numero").ToString(); // novo Numero
			txtCliente_numCartao.Text = getNewID("Cartao", "Num_cartao").ToString(); // novo Num_cartao
			chkboxCliente_ativo.IsChecked = true;
			if (App.Current.Properties["USER_TYPE"].Equals("A")) // se for admin pode alterar
				chkboxCliente_ativo.IsEnabled = true;
			else
				chkboxCliente_ativo.IsEnabled = false;
			txtCliente_pontos.Text = "0";
			txtCliente_nome.Focus();
			clienteUltimoBotao = 'A';
		}

		private void btnCliente_limpar_Click(object sender, RoutedEventArgs e)
		{
			txtCliente_codigo.Clear();
			txtCliente_nome.Clear();
			txtCliente_email.Clear();
			dateCliente_dNasc.Text = "";
			txtCliente_endereco.Clear();
			txtCliente_tele.Clear();
			txtCliente_nif.Clear();
			txtCliente_numCartao.Clear();
			txtCliente_pontos.Clear();
			txtCliente_nome.IsEnabled = false;
			txtCliente_tele.IsEnabled = false;
			txtCliente_email.IsEnabled = false;
			txtCliente_endereco.IsEnabled = false;
			txtCliente_nif.IsEnabled = false;
			dateCliente_dNasc.IsEnabled = false;
			chkboxCliente_ativo.IsChecked = false;
			chkboxCliente_ativo.IsEnabled = false;
			btnCliente_limpar.IsEnabled = true;
			btnCliente_eliminar.IsEnabled = false;
			btnCliente_confirmar.IsEnabled = false;
			btnCliente_alterar.IsEnabled = false;
			btnCliente_adicionar.IsEnabled = true;
			lblCliente_aviso.Visibility = Visibility.Hidden;
			txtCliente_search.Clear();
			txtCliente_search.Focus();
		}

		private void textbox_cliente_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtCliente_search.Text != "")
				btnCliente_search.IsEnabled = true;
			else
				btnCliente_search.IsEnabled = false;
		}

		private void txtCliente_search_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) // pesquisar com a tecla Enter
				btnCliente_search_Click(sender, e);
		}
		/***************************************** FIM TAB CLIENTE *****************************************/

		/***************************************** GERIR FUNCIONARIO *****************************************/
		private void btnFunc_search_Click(object sender, RoutedEventArgs e)
		{
			btnFunc_limpar_Click(sender, e);
			//lblFunc_avisos.Visibility = Visibility.Hidden;
			try
			{
				string searchType;
				if (cboFunc_search.SelectedIndex == 0) // by num funcionario
					searchType = "Numero";
				else if (cboFunc_search.SelectedIndex == 1) // by username
					searchType = "Username";
				else if (cboFunc_search.SelectedIndex == 2) // by email
					searchType = "Email";
				else // by nif
					searchType = "NIF";
				string sql = "SELECT * FROM PROJECTO.Funcionario WHERE " + searchType + "='" + txtFunc_search.Text + "'";

				SqlCommand query = new SqlCommand(sql, myConnection);
				myConnection.Open();
				SqlDataReader myReader = query.ExecuteReader();

				if (myReader.Read())
				{
					txtFunc_codigo.Text = myReader["Numero"].ToString();
					txtFunc_nome.Text = myReader["Nome"].ToString();
					txtFunc_email.Text = myReader["Email"].ToString();
					dateFunc_dNasc.Text = myReader["Data_nasc"].ToString();
					txtFunc_endereco.Text = myReader["Endereco"].ToString();
					txtFunc_tele.Text = myReader["Telefone"].ToString();
					txtFunc_nif.Text = myReader["NIF"].ToString();
					txtFunc_numLoja.Text = myReader["Codigo_loja"].ToString();
					txtFunc_username.Text = myReader["Username"].ToString();
					if (App.Current.Properties["USER_TYPE"].Equals("A")) // apenas administradores podem ver as passwords
						txtFunc_password.Text = myReader["Password"].ToString();
					chkboxFunc_ativo.IsChecked = ((myReader["Ativo"].ToString().Equals("True")) ? true : false);
				}
				else
					throw new Exception("Funcionário inexistente!");
			}
			catch (Exception ex)
			{
				btnFunc_limpar_Click(sender, e);
				lblFunc_avisos.Content = "Erro! " + ex.Message;
				lblFunc_avisos.Foreground = new SolidColorBrush(Colors.DarkRed);
				lblFunc_avisos.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();

				btnFunc_confirmar.IsEnabled = false;
				btnFunc_adicionar.IsEnabled = false;
				btnFunc_limpar.IsEnabled = true;
				btnFunc_alterar.IsEnabled = true;
				if (App.Current.Properties["USER_TYPE"].ToString().Equals("A")) // se for administrador
					btnFunc_eliminar.IsEnabled = true;
				txtFunc_search.Clear();
				txtFunc_search.Focus();
			}
		}

		private void btnFunc_confirmar_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string sql;
				SqlCommand query;
				if (funcionarioUltimoBotao == 'A') // adicionar um funcionario
				{
					if (txtFunc_codigo.Text.Equals("-1"))
						throw new Exception("Nao foi possível obter novo número de funcionário.");

					string tipoFuncionario = ((cboFunc_tipoUser.SelectedIndex == 0) ? "A" : "F");
					string ativoFuncionario = ((chkboxFunc_ativo.IsChecked.ToString().Equals("True")) ? "1" : "0");
					string numLojaFuncionario = App.Current.Properties["NUM_LOJA"].ToString();
					sql = "INSERT INTO PROJECTO.Funcionario (Nome,Endereco,Email,Telefone,Numero,NIF,Data_nasc,Codigo_loja,Username,Password,Tipo,Ativo) VALUES ('" + txtFunc_nome.Text + "','" + txtFunc_endereco.Text + "','" + txtFunc_email.Text + "'," + txtFunc_tele.Text + "," + txtFunc_codigo.Text + "," + txtFunc_nif.Text + ",'" + dateFunc_dNasc.Text + "'," + numLojaFuncionario + ",'" + txtFunc_username.Text + "','" + txtFunc_password.Text + "','" + tipoFuncionario + "'," + ativoFuncionario + ")";

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível adicionar um novo Funcionário.");

					lblFunc_avisos.Content = "Funcionário adicionado com sucesso!";
				}
				else // alterar um funcionario
				{
					string tipoFuncionario = ((cboFunc_tipoUser.SelectedIndex == 0) ? "A" : "F");
					string ativoFuncionario = ((chkboxFunc_ativo.IsChecked.ToString().Equals("True")) ? "1" : "0");
					string numLojaFuncionario = App.Current.Properties["NUM_LOJA"].ToString();
					sql = "UPDATE PROJECTO.Funcionario SET Nome='" + txtFunc_nome.Text + "', Endereco='" + txtFunc_endereco.Text + "', Email='" + txtFunc_email.Text + "', Telefone='" + txtFunc_tele.Text + "', NIF='" + txtFunc_nif.Text + "', Data_nasc='" + dateFunc_dNasc.Text + "', Codigo_loja='" + txtFunc_numLoja.Text + "', Username='" + txtFunc_username.Text + "', Password='" + txtFunc_password.Text + "', Tipo='" + tipoFuncionario + "', Ativo='" + ativoFuncionario + "' WHERE Numero='" + txtFunc_codigo.Text + "'";

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível alterar o Funcionário.");

					lblFunc_avisos.Content = "Funcionário alterado com sucesso!";
				}

				lblFunc_avisos.Foreground = new SolidColorBrush(Colors.DarkGreen);
				lblFunc_avisos.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				btnFunc_limpar_Click(sender, e);
				lblFunc_avisos.Content = "Erro! " + ex.Message;
				lblFunc_avisos.Foreground = new SolidColorBrush(Colors.DarkRed);
				lblFunc_avisos.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();
				btnFunc_limpar_Click(sender, e);
				lblFunc_avisos.Foreground = new SolidColorBrush(Colors.DarkGreen);
				lblFunc_avisos.Visibility = Visibility.Visible;
			}
		}

		private void btnFunc_alterar_Click(object sender, RoutedEventArgs e)
		{
			funcionarioUltimoBotao = 'T';
			btnFunc_confirmar.IsEnabled = true;
			btnFunc_adicionar.IsEnabled = false;
			btnFunc_alterar.IsEnabled = false;
			btnFunc_eliminar.IsEnabled = false;

			txtFunc_nome.IsEnabled = true;
			txtFunc_email.IsEnabled = true;
			dateFunc_dNasc.IsEnabled = true;
			txtFunc_endereco.IsEnabled = true;
			txtFunc_tele.IsEnabled = true;
			txtFunc_nif.IsEnabled = true;
			txtFunc_username.IsEnabled = true;
			txtFunc_password.IsEnabled = true;
			if (App.Current.Properties["USER_TYPE"].Equals("A")) // apenas administradores
			{
				cboFunc_tipoUser.IsEnabled = true;
				chkboxFunc_ativo.IsEnabled = true;
			}
			txtFunc_nome.Focus();
		}

		private void btnFunc_adicionar_Click(object sender, RoutedEventArgs e)
		{
			funcionarioUltimoBotao = 'A';
			btnFunc_limpar_Click(sender, e);
			btnFunc_confirmar.IsEnabled = true;
			btnFunc_adicionar.IsEnabled = false;
			btnFunc_alterar.IsEnabled = false;
			btnFunc_eliminar.IsEnabled = false;
			txtFunc_codigo.Text = getNewID("Funcionario", "Numero").ToString();
			txtFunc_numLoja.Text = App.Current.Properties["NUM_LOJA"].ToString();
			txtFunc_nome.IsEnabled = true;
			txtFunc_email.IsEnabled = true;
			dateFunc_dNasc.IsEnabled = true;
			txtFunc_endereco.IsEnabled = true;
			txtFunc_tele.IsEnabled = true;
			txtFunc_nif.IsEnabled = true;
			txtFunc_username.IsEnabled = true;
			txtFunc_password.IsEnabled = true;
			cboFunc_tipoUser.IsEnabled = true;
			cboFunc_tipoUser.SelectedIndex = 1;
			chkboxFunc_ativo.IsEnabled = true;
			chkboxFunc_ativo.IsChecked = true;
			txtFunc_nome.Focus();
		}

		private void btnFunc_eliminar_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult res = MessageBox.Show("Tem a certeza que deseja eliminar?", "Confirmar a eliminação", MessageBoxButton.YesNo);
			if (res == MessageBoxResult.Yes) // Sim
			{
				try
				{
					string sql;
					SqlCommand query;

					sql = "UPDATE PROJECTO.Funcionario SET Ativo='0' WHERE Numero='" + txtFunc_codigo.Text + "'";

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível eliminar o Funcionário.");
				}
				catch (Exception ex)
				{
					btnFunc_limpar_Click(sender, e);
					lblFunc_avisos.Content = "Erro! " + ex.Message;
					lblFunc_avisos.Foreground = new SolidColorBrush(Colors.DarkRed);
					lblFunc_avisos.Visibility = Visibility.Visible;
				}
				finally
				{
					myConnection.Close();
					btnFunc_limpar_Click(sender, e);
					lblFunc_avisos.Content = "Funcionário eliminado com sucesso!";
					lblFunc_avisos.Foreground = new SolidColorBrush(Colors.DarkGreen);
					lblFunc_avisos.Visibility = Visibility.Visible;
				}
			}
		}

		private void btnFunc_limpar_Click(object sender, RoutedEventArgs e)
		{
			btnFunc_confirmar.IsEnabled = false;
			btnFunc_adicionar.IsEnabled = true;
			btnFunc_alterar.IsEnabled = false;
			btnFunc_eliminar.IsEnabled = false;
			btnFunc_limpar.IsEnabled = true;
			txtFunc_codigo.Clear();
			txtFunc_nome.Clear();
			txtFunc_email.Clear();
			dateFunc_dNasc.Text = "";
			txtFunc_endereco.Clear();
			txtFunc_tele.Clear();
			txtFunc_nif.Clear();
			txtFunc_numLoja.Clear();
			txtFunc_username.Clear();
			txtFunc_password.Clear();
			chkboxFunc_ativo.IsChecked = false;
			lblFunc_avisos.Visibility = Visibility.Hidden;
			cboFunc_tipoUser.IsEnabled = false;
			txtFunc_codigo.IsEnabled = false;
			txtFunc_nome.IsEnabled = false;
			txtFunc_email.IsEnabled = false;
			dateFunc_dNasc.IsEnabled = false;
			txtFunc_endereco.IsEnabled = false;
			txtFunc_tele.IsEnabled = false;
			txtFunc_nif.IsEnabled = false;
			txtFunc_numLoja.IsEnabled = false;
			txtFunc_username.IsEnabled = false;
			txtFunc_password.IsEnabled = false;
			chkboxFunc_ativo.IsEnabled = false;
			txtFunc_search.Focus();
		}

		private void txtFunc_search_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) // pesquisar com a tecla Enter
				btnFunc_search_Click(sender, e);
		}

		private void txtFunc_search_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtFunc_search.Text != "")
				btnFunc_search.IsEnabled = true;
			else
				btnFunc_search.IsEnabled = false;
		}
		/***************************************** FIM GERIR FUNCIONARIO *****************************************/

		private void radioHistVendas_Checked(object sender, RoutedEventArgs e)
		{
			cbhist_tipo.IsEnabled = true;
			txtHistSearch.IsEnabled = true;
			aviso_hist.Visibility = Visibility.Hidden;
			try
			{
				string sql = "SELECT * FROM PROJECTO.Venda";
				SqlDataAdapter dataadapter = new SqlDataAdapter(sql, myConnection);
				myConnection.Open();
				DataTable t = new DataTable();
				dataadapter.Fill(t);
				datagridHist.ItemsSource = t.DefaultView;
			}
			catch (Exception ex)
			{
				aviso_hist.Content = "Erro" + ex;
				aviso_hist.Visibility = Visibility.Visible;
			}
			myConnection.Close();

		}

		private void radioHistCompras_Checked(object sender, RoutedEventArgs e)
		{
			cbhist_tipo.IsEnabled = true;
			txtHistSearch.IsEnabled = true;
			aviso_hist.Visibility = Visibility.Hidden;
			try
			{
				string sql = "SELECT * FROM PROJECTO.Compra";
				SqlDataAdapter dataadapter = new SqlDataAdapter(sql, myConnection);
				myConnection.Open();
				DataTable t = new DataTable();
				dataadapter.Fill(t);
				datagridHist.ItemsSource = t.DefaultView;
			}
			catch (Exception ex)
			{
				aviso_hist.Content = "Erro" + ex;
				aviso_hist.Visibility = Visibility.Visible;
			}
			myConnection.Close();
		}

		private void radioHistReservas_Checked(object sender, RoutedEventArgs e)
		{
			cbhist_tipo.IsEnabled = true;
			txtHistSearch.IsEnabled = true;
			aviso_hist.Visibility = Visibility.Hidden;
			try
			{
				string sql = "SELECT * FROM PROJECTO.Reserva";
				SqlDataAdapter dataadapter = new SqlDataAdapter(sql, myConnection);
				myConnection.Open();
				DataTable t = new DataTable();
				dataadapter.Fill(t);
				datagridHist.ItemsSource = t.DefaultView;
			}
			catch (Exception ex)
			{
				aviso_hist.Content = "Erro" + ex;
				aviso_hist.Visibility = Visibility.Visible;
			}
			myConnection.Close();
		}

		private void txtHistSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtHistSearch.Text != "")
				btnHistSearch.IsEnabled = true;
			else
				btnHistSearch.IsEnabled = false;

		}

		private void btnHistSearch_Click(object sender, RoutedEventArgs e)
		{
			string sql = null;
			if (cbhist_tipo.SelectedIndex == 0)
			{
				try
				{
					if (radioHistReservas.IsChecked == true)
						sql = "SELECT * FROM PROJECTO.Reserva WHERE Num_cliente=" + txtHistSearch.Text + ";";
					else if (radioHistVendas.IsChecked == true)
						sql = "SELECT * FROM PROJECTO.Venda WHERE Num_cliente=" + txtHistSearch.Text + ";";
					else if (radioHistCompras.IsChecked == true)
						sql = "SELECT * FROM PROJECTO.Compra WHERE Num_cliente=" + txtHistSearch.Text + ";";

					SqlDataAdapter dataadapter = new SqlDataAdapter(sql, myConnection);
					myConnection.Open();
					DataTable t = new DataTable();
					dataadapter.Fill(t);
					datagridHist.ItemsSource = t.DefaultView;
				}
				catch (Exception ex)
				{
					aviso_hist.Content = "Erro" + ex;
					aviso_hist.Visibility = Visibility.Visible;
				}
				myConnection.Close();
			}
			else if (cbhist_tipo.SelectedIndex == 1)
			{
				try
				{
					if (radioHistReservas.IsChecked == true)
						sql = "SELECT * FROM PROJECTO.Reserva WHERE Referencia_produto=" + txtHistSearch.Text + ";";
					else if (radioHistVendas.IsChecked == true)
						sql = "SELECT * FROM PROJECTO.Venda WHERE Referencia_produto=" + txtHistSearch.Text + ";";
					else if (radioHistCompras.IsChecked == true)
						sql = "SELECT * FROM PROJECTO.Compra WHERE Referencia_produto=" + txtHistSearch.Text + ";";

					SqlDataAdapter dataadapter = new SqlDataAdapter(sql, myConnection);
					myConnection.Open();
					DataTable t = new DataTable();
					dataadapter.Fill(t);
					datagridHist.ItemsSource = t.DefaultView;
				}
				catch (Exception ex)
				{
					aviso_hist.Content = "Erro" + ex;
					aviso_hist.Visibility = Visibility.Visible;
				}
				myConnection.Close();
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			tab_cliente.IsSelected = true;
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			tab_produto.IsSelected = true;
		}


		// *********************** TAB VENDA »»********************
		private void botao_pesquisar_cliente_vender_Click(object sender, RoutedEventArgs e)
		{
			lblVenderClienteAviso.Visibility = Visibility.Hidden;
			try
			{
				string searchType = "";
				if (combobox_cliente_vender.SelectedIndex == 0) // by num cliente
					searchType = "Numero";
				else if (combobox_cliente_vender.SelectedIndex == 1) // by num cartao
					searchType = "Num_cartao";
				else // by nif
					searchType = "NIF";

				string sql = "SELECT * FROM PROJECTO.Cliente WHERE PROJECTO.Cliente." + searchType + "='" + textbox_cliente_vender.Text + "'";

				SqlCommand query = new SqlCommand(sql, myConnection);
				myConnection.Open();
				SqlDataReader myReader = query.ExecuteReader();
				if (myReader.Read())
				{
					textbox_num_cliente_vender.Text = myReader["Numero"].ToString();
					textbox_nome_cliente_vender.Text = myReader["Nome"].ToString();
					textbox_email_cliente_vender.Text = myReader["Email"].ToString();
					textbox_endereco_cliente_vender.Text = myReader["Endereco"].ToString();
					textbox_tel_cliente_vender.Text = myReader["Telefone"].ToString();
					textbox_nif_cliente_vender.Text = myReader["NIF"].ToString();

				}
				else
					throw new Exception("Cliente inexistente!");
			}
			catch (Exception ex)
			{
				lblVenderClienteAviso.Content = "ERRO! " + ex.Message;
				lblVenderClienteAviso.Visibility = Visibility.Visible;
				textbox_cliente_vender.Focus();
			}
			finally
			{
				myConnection.Close();
				textbox_cliente_vender.Text = "";
			}
		}

		private void botao_pesquisar_produto_vender_Click(object sender, RoutedEventArgs e)
		{

			lblVenderProdutoAviso.Visibility = Visibility.Hidden;
			try
			{

				string sql = "SELECT Nome, Preco,Nome_fabricante, Genero, Plataforma FROM (PROJECTO.Produto FULL OUTER JOIN PROJECTO.Jogo ON PROJECTO.Produto.Referencia=PROJECTO.Jogo.Referencia) WHERE PROJECTO.Produto.Referencia='" + textbox_produto_vender.Text + "'";

				SqlCommand query = new SqlCommand(sql, myConnection);

				myConnection.Open();
				SqlDataReader myReader = query.ExecuteReader();


				if (myReader.Read())
				{
					textbox_ref_vender.Text = textbox_produto_vender.Text;
					textbox_nome_produto_vender.Text = myReader["Nome"].ToString();
					textbox_preco_produto_vender.Text = myReader["Preco"].ToString();
					textbox_fabricante_produto_vender.Text = myReader["Nome_fabricante"].ToString();
					textbox_plataforma_produto_vender.Text = myReader["Plataforma"].ToString();
					textbox_genero_produto_vender.Text = myReader["Genero"].ToString();

				}
				else
					throw new Exception("Produto inexistente!");
			}
			catch (Exception ex)
			{
				lblVenderProdutoAviso.Content = "ERRO! " + ex.Message;
				lblVenderProdutoAviso.Visibility = Visibility.Visible;
				textbox_produto_vender.Focus();
			}
			finally
			{
				myConnection.Close();
				textbox_produto_vender.Text = "";
			}
		}

		private void botao_confirmar_vender_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string sql = "";
				string num_pag = getNewID("Pagamento", "Num_transacção").ToString();
				string num_ven = getNewID("Venda", "Num_venda").ToString();
				//MessageBox.Show("num" + App.Current.Properties["NUMERO_FUNC"]);
				// criar um novo pagamento antes de inserir uma venda
				if (combobox_pagamento_vender.SelectedIndex == 0)
					sql = "INSERT INTO PROJECTO.Pagamento (Tipo, Num_transacção,Conta_origem,Conta_destino) VALUES ('" + combobox_pagamento_vender.Text + "','" + num_pag + "',NULL,NULL)";
				else
					sql = "INSERT INTO PROJECTO.Pagamento (Tipo, Num_transacção,Conta_origem,Conta_destino) VALUES ('" + combobox_pagamento_vender.Text + "','" + num_pag + "','" + conta_origem_vender.Text + "','" + conta_destino_vender.Text + "')";


				SqlCommand query1 = new SqlCommand(sql, myConnection);
				myConnection.Open();
				if (query1.ExecuteNonQuery() != 1)
					throw new Exception("Nao foi possível proceder com o pagamento.");
				myConnection.Close();

				// criar nova venda e associar um pagamento



				string sql2 = "INSERT INTO PROJECTO.Venda (Data,Num_venda,Num_cliente,Num_funcionario,Referencia_produto,Num_transacção) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + num_ven + "," + textbox_num_cliente_vender.Text + "," + App.Current.Properties["NUMERO_FUNC"].ToString() + ",'" + textbox_ref_vender.Text + "'," + num_pag + ");";

				//string sql2 = "INSERT INTO PROJECTO.Venda (Data,Num_venda,Num_cliente,Num_funcionario,Referencia_produto,Num_transacção) VALUES (getDate(),25,1,1,'20444',39)";
				//MessageBox.Show(sql2);
				SqlCommand query2 = new SqlCommand(sql2, myConnection);
				myConnection.Open();
				if (query2.ExecuteNonQuery() != 1)
					throw new Exception("Nao foi possível criar nova venda.");

				aviso_vender.Content = "Sucesso!";
				aviso_vender.Foreground = new SolidColorBrush(Colors.DarkGreen);
				aviso_vender.Visibility = Visibility.Visible;
			}
			catch (Exception ex)
			{
				aviso_vender.Content = "Erro! " + ex;
				btnCliente_limpar_Click(sender, e);
				aviso_vender.Foreground = new SolidColorBrush(Colors.DarkRed);
				aviso_vender.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();
				Button_Click_2(sender, e);
				aviso_vender.Visibility = Visibility.Visible;

			}

		}

		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			textbox_num_cliente_vender.Clear();
			textbox_nome_cliente_vender.Clear();
			textbox_email_cliente_vender.Clear();
			textbox_endereco_cliente_vender.Clear();
			textbox_tel_cliente_vender.Clear();
			textbox_nif_cliente_vender.Clear();
			textbox_ref_vender.Clear();
			textbox_nome_produto_vender.Clear();
			textbox_preco_produto_vender.Clear();
			textbox_fabricante_produto_vender.Clear();
			textbox_plataforma_produto_vender.Clear();
			textbox_genero_produto_vender.Clear();
			botao_confirmar_vender.IsEnabled = false;
		}

		/***************************************** TAB PRODUTOS *****************************************/
		private void btnProduto_search_Click(object sender, RoutedEventArgs e)
		{
			//btnProduto_limpar_Click(sender, e);
			try
			{
				string searchType;
				if (cboFunc_search.SelectedIndex == 0) // by referencia
					searchType = "Referencia";
				else // by default
					searchType = "Referencia";
				string sql = "SELECT * FROM PROJECTO.Produto WHERE " + searchType + "='" + txtProduto_search.Text + "'";

				SqlCommand query = new SqlCommand(sql, myConnection);
				myConnection.Open();
				SqlDataReader myReader = query.ExecuteReader();

				if (myReader.Read())
				{
					txtProduto_ref.Text = myReader["Referencia"].ToString();
					txtProduto_nome.Text = myReader["Nome"].ToString();
					txtProduto_preco.Text = myReader["Preco"].ToString();
					txtProduto_estado.Text = myReader["Novo_usado"].ToString();
					txtProduto_plataforma.Text = myReader["Plataforma"].ToString();
					txtProduto_desc.Text = myReader["Descricao"].ToString();
					cboProduto_fabricante.Items.Add(myReader["Nome_fabricante"].ToString());
					cboProduto_fabricante.SelectedIndex = 0;
				}
				else
					throw new Exception("Produto nao encontrado ou inexistente!");
			}
			catch (Exception ex)
			{
				btnProduto_limpar_Click(sender, e);
				lblProduto_aviso.Content = "Erro! " + ex.Message;
				lblProduto_aviso.Foreground = new SolidColorBrush(Colors.DarkRed);
				lblProduto_aviso.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();
				btnProduto_confirmar.IsEnabled = false;
				btnProduto_adicionar.IsEnabled = false;
				btnProduto_alterar.IsEnabled = true;
				btnProduto_eliminar.IsEnabled = true;
				btnProduto_limpar.IsEnabled = true;
				txtProduto_search.Clear();
			}
		}

		private void btnProduto_confirmar_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string sql;
				SqlCommand query;

				if (produtoUltimoBotao == 'A') // adicionar um novo produto
				{
					if (txtProduto_ref.Text.Equals("-1"))
						throw new Exception("Não foi possível obter nova referência");

					string fabricante = cboProduto_fabricante.SelectedItem.ToString();
					sql = "INSERT INTO PROJECTO.Produto (Nome, Preco, Referencia, Novo_usado, Plataforma, Descricao, Nome_fabricante, Codigo_loja) VALUES ('" + txtProduto_nome.Text + "','" + txtProduto_preco.Text + "','" + txtProduto_ref.Text + "','" + txtProduto_estado.Text + "', '" + txtProduto_plataforma.Text + "','" + txtProduto_desc.Text + "','" + fabricante + "', '" + App.Current.Properties["NUM_LOJA"].ToString() + "')";

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível adicionar o novo Produto.");

					lblProduto_aviso.Content = "Produto adicionado com sucesso";
				}
				else // alterar um produto
				{
					string fabricante = cboProduto_fabricante.SelectedItem.ToString();
					sql = "UPDATE PROJECTO.Produto SET Nome='" + txtProduto_nome.Text + "', Preco='" + txtProduto_preco.Text + "', Novo_usado='" + txtProduto_estado.Text + "', Plataforma='" + txtProduto_plataforma.Text + "', Descricao='" + txtProduto_desc.Text + "', Nome_fabricante='" + fabricante + "' WHERE Referencia='" + txtProduto_ref.Text + "'";

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível alterar o Produto.");

					lblProduto_aviso.Content = "Produto alterado com sucesso";
				}
			}
			catch (Exception ex)
			{
				btnProduto_limpar_Click(sender, e);
				lblProduto_aviso.Content = "Erro! " + ex.Message;
				lblProduto_aviso.Foreground = new SolidColorBrush(Colors.DarkRed);
				lblProduto_aviso.Visibility = Visibility.Visible;
			}
			finally
			{
				myConnection.Close();
				btnProduto_limpar_Click(sender, e);
				lblProduto_aviso.Foreground = new SolidColorBrush(Colors.DarkGreen);
				lblProduto_aviso.Visibility = Visibility.Visible;
				btnProduto_confirmar.IsEnabled = false;
				btnProduto_adicionar.IsEnabled = true;
				btnProduto_alterar.IsEnabled = false;
				btnProduto_eliminar.IsEnabled = false;
				btnProduto_limpar.IsEnabled = true;
			}
		}

		private void btnProduto_adicionar_Click(object sender, RoutedEventArgs e)
		{
			produtoUltimoBotao = 'A';
			btnProduto_limpar_Click(sender, e);
			txtProduto_ref.IsEnabled = false;
			txtProduto_ref.Text = getNewID("Produto", "Referencia").ToString();
			txtProduto_nome.IsEnabled = true;
			txtProduto_preco.IsEnabled = true;
			txtProduto_estado.IsEnabled = true;
			txtProduto_plataforma.IsEnabled = true;
			txtProduto_desc.IsEnabled = true;
			cboProduto_fabricante.IsEnabled = true;
			btnProduto_search.IsEnabled = true;
			btnProduto_confirmar.IsEnabled = true;
			btnProduto_adicionar.IsEnabled = false;
			btnProduto_alterar.IsEnabled = false;
			btnProduto_eliminar.IsEnabled = false;
			btnProduto_limpar.IsEnabled = true;
			txtProduto_nome.Focus();
			fillProduto_fabricantes(sender, e);
		}

		private void btnProduto_alterar_Click(object sender, RoutedEventArgs e)
		{
			produtoUltimoBotao = 'T';
			txtProduto_ref.IsEnabled = false;
			txtProduto_nome.IsEnabled = true;
			txtProduto_preco.IsEnabled = true;
			txtProduto_estado.IsEnabled = true;
			txtProduto_plataforma.IsEnabled = true;
			txtProduto_desc.IsEnabled = true;
			cboProduto_fabricante.IsEnabled = true;
			btnProduto_search.IsEnabled = true;
			btnProduto_confirmar.IsEnabled = true;
			btnProduto_adicionar.IsEnabled = false;
			btnProduto_alterar.IsEnabled = false;
			btnProduto_eliminar.IsEnabled = false;
			btnProduto_limpar.IsEnabled = true;
			fillProduto_fabricantes(sender, e);
			txtProduto_nome.Focus();
		}

		private void fillProduto_fabricantes(object sender, RoutedEventArgs e)
		{
			// preencher combobox fabricantes
			try
			{
				string sql = "SELECT Nome FROM PROJECTO.Fabricante";
				SqlCommand query = new SqlCommand(sql, myConnection);
				myConnection.Open();
				SqlDataReader myReader = query.ExecuteReader();
				while (myReader.Read())
				{
					cboProduto_fabricante.Items.Add(myReader["Nome"].ToString());
				}
			}
			catch (SqlException ex)
			{
				btnFunc_limpar_Click(sender, e);
				lblProduto_aviso.Content = "Erro! " + ex.Message;
				lblProduto_aviso.Foreground = new SolidColorBrush(Colors.DarkRed);
				lblProduto_aviso.Visibility = Visibility.Visible;
			}
			finally { myConnection.Close(); }
		}

		private void btnProduto_eliminar_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult res = MessageBox.Show("Tem a certeza que deseja eliminar?", "Confirmar a eliminação", MessageBoxButton.YesNo);
			if (res == MessageBoxResult.Yes) // Sim
			{
				try
				{
					string sql;
					SqlCommand query;

					sql = "DELETE FROM PROJECTO.Produto WHERE Referencia='" + txtProduto_ref.Text + "'";

					query = new SqlCommand(sql, myConnection);
					myConnection.Open();
					if (query.ExecuteNonQuery() != 1)
						throw new Exception("Nao foi possível eliminar o Produto.");

					lblProduto_aviso.Content = "Produto eliminado com sucesso!";
				}
				catch (Exception ex)
				{
					btnProduto_limpar_Click(sender, e);
					lblProduto_aviso.Content = "Erro! " + ex.Message;
					lblProduto_aviso.Foreground = new SolidColorBrush(Colors.DarkRed);
					lblProduto_aviso.Visibility = Visibility.Visible;
				}
				finally
				{
					myConnection.Close();
					btnProduto_limpar_Click(sender, e);
					lblProduto_aviso.Foreground = new SolidColorBrush(Colors.DarkGreen);
					lblProduto_aviso.Visibility = Visibility.Visible;
					btnProduto_confirmar.IsEnabled = false;
					btnProduto_adicionar.IsEnabled = true;
					btnProduto_alterar.IsEnabled = false;
					btnProduto_eliminar.IsEnabled = false;
					btnProduto_limpar.IsEnabled = true;
				}
			}
		}

		private void btnProduto_limpar_Click(object sender, RoutedEventArgs e)
		{
			txtProduto_search.Clear();
			txtProduto_ref.Clear();
			txtProduto_nome.Clear();
			txtProduto_preco.Clear();
			txtProduto_estado.Clear();
			txtProduto_plataforma.Clear();
			txtProduto_desc.Clear();
			cboProduto_fabricante.Items.Clear();
			txtProduto_search.IsEnabled = true;
			txtProduto_ref.IsEnabled = false;
			txtProduto_nome.IsEnabled = false;
			txtProduto_preco.IsEnabled = false;
			txtProduto_estado.IsEnabled = false;
			txtProduto_plataforma.IsEnabled = false;
			txtProduto_desc.IsEnabled = false;
			cboProduto_fabricante.IsEnabled = false;
			btnProduto_search.IsEnabled = true;
			btnProduto_confirmar.IsEnabled = false;
			btnProduto_adicionar.IsEnabled = true;
			btnProduto_alterar.IsEnabled = false;
			btnProduto_eliminar.IsEnabled = false;
			btnProduto_limpar.IsEnabled = true;
			txtProduto_search.Focus();
		}

		private void txtProduto_search_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (txtProduto_search.Text.Equals(""))
				btnProduto_search.IsEnabled = false;
			else
				btnProduto_search.IsEnabled = true;
		}

		private void txtProduto_search_KeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Enter) && (btnProduto_search.IsEnabled == true)) // mesma acao que o click do pesquisar
				btnProduto_search_Click(sender, e);
		}
		/***************************************** FIM TAB PRODUTOS *****************************************/
	}
}
