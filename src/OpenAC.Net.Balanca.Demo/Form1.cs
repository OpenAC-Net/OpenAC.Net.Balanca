using System;
using System.IO.Ports;
using System.Windows.Forms;
using OpenAC.Net.Devices;

namespace OpenAC.Net.Balanca.Demo
{
    public partial class Form1 : Form
    {
        #region Fields

        private OpenBal<SerialConfig> balanca;

        #endregion Fields

        #region Constructors

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            balanca = new OpenBal<SerialConfig>();
            balanca.AoLerPeso += Balanca_AoLerPeso;

            comboBox1.DataSource = SerialPort.GetPortNames();
            comboBox2.DataSource = Enum.GetValues(typeof(ProtocoloBalanca));
        }

        #endregion Constructors

        #region EventHandlers

        private void btnConectar_Click(object sender, EventArgs e)
        {
            //Desconecta antes
            if (!balanca.Conectado)
            {
                balanca.Protocolo = (ProtocoloBalanca)Convert.ToInt32(comboBox2.SelectedValue);
                balanca.DelayMonitoramento = (int)numericUpDown3.Value;
                balanca.Device.Porta = comboBox1.Text;
                balanca.Device.Baud = (int)numericUpDown1.Value;
                balanca.Device.TimeOut = (int)numericUpDown2.Value;
                balanca.Device.ControlePorta = true;

                balanca.Conectar();
                btnConectar.Text = @"Desconectar";
            }
            else
            {
                balanca.Desconectar();
                btnConectar.Text = @"Conectar";
            }
        }

        private void btnLerPeso_Click(object sender, EventArgs e)
        {
            if (balanca is not { Conectado: true })
            {
                MessageBox.Show(@"Balança não conectada");
                return;
            }

            balanca.LerPeso();
        }

        private void chkMonitorar_CheckedChanged(object sender, EventArgs e) => balanca.IsMonitorar = chkMonitorar.Checked;

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

        #endregion EventHandlers
    }
}