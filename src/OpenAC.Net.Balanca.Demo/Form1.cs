using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace OpenAC.Net.Balanca.Demo
{
    public partial class Form1 : Form
    {
        private OpenBal Balanca;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Balanca = new OpenBal();
            Balanca.AoLerPeso += Balanca_AoLerPeso;

            comboBox1.DataSource = SerialPort.GetPortNames();
            comboBox2.DataSource = Enum.GetValues(typeof(ProtocoloBalanca));
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            //Desconecta antes
            if (!Balanca.IsConectado)
            {
                Balanca.Config.Porta = comboBox1.Text;
                Balanca.Config.Protocolo = (ProtocoloBalanca)Convert.ToInt32(comboBox2.SelectedValue);
                Balanca.Config.Baud = (int)numericUpDown1.Value;
                Balanca.Config.TimeOut = (int)numericUpDown2.Value;
                Balanca.Config.DelayMonitoramento = (int)numericUpDown3.Value;
                Balanca.Config.ControlePorta = false;

                Balanca.Conectar();
                btnConectar.Text = @"Desconectar";
            }
            else
            {
                Balanca.Desconectar();
                btnConectar.Text = @"Conectar";
            }
        }

        private void btnLerPeso_Click(object sender, EventArgs e)
        {
            if (Balanca is not { IsConectado: true })
            {
                MessageBox.Show(@"Balança não conectada");
                return;
            }

            Balanca.LerPeso();
        }

        private void chkMonitorar_CheckedChanged(object sender, EventArgs e)
        {
            Balanca.Config.IsMonitorar = chkMonitorar.Checked;
        }

        private void Balanca_AoLerPeso(object sender, BalancaEventArgs e)
        {
            if (e.Peso.HasValue)
            {
                label7.Text = $@"Ultimo peso {e.Peso:N3} Kg";
                textBox1.Text += $@"{DateTime.Now:dd/MM/yyyy HH:mm:ss} - {e.Peso:N3} Kg" + Environment.NewLine;
            }

            if (e.Excecao != null)
            {
                textBox2.Text += $@"{DateTime.Now:dd/MM/yyyy HH:mm:ss} - {e.Excecao.Message}" + Environment.NewLine;
            }

            Application.DoEvents();
        }
    }
}