using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QLDSV_TC.views
{
    public partial class frmLogin : DevExpress.XtraEditors.XtraForm
    {
        private SqlConnection conn_publisher = new SqlConnection();
        String loginNameSV = "";


        private void LayDSPM(String cmd)
        {
            DataTable dt = new DataTable();

            if (conn_publisher.State == ConnectionState.Closed) conn_publisher.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd, conn_publisher);
            da.Fill(dt);

            conn_publisher.Close();
            Program.bdsDSPM.DataSource = dt;
            cmbKhoa.DataSource = Program.bdsDSPM;
            cmbKhoa.DisplayMember = "TENPHONG";
            cmbKhoa.ValueMember = "TENSERVER";
        }

        private int KetNoi_CSDLGoc()
        {
            if (conn_publisher != null && conn_publisher.State == ConnectionState.Open)
                conn_publisher.Close();
            try
            {
                conn_publisher.ConnectionString = Program.constr_publisher;
                conn_publisher.Open();
                return 1;
            }
            catch (Exception e)
            {
                MessageBox.Show("Lỗi kết nối\n" + e.Message);
                return 0;
            }
        }

        public frmLogin()
        {

            InitializeComponent();
        }

        private void hideshowpass_CheckedChanged(object sender, EventArgs e)
        {

            if (hideshowpass.Checked)
            {
                txtPass.Properties.UseSystemPasswordChar = false;
            }
            else
            {
                txtPass.Properties.UseSystemPasswordChar = true;
            }

        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            if (KetNoi_CSDLGoc() == 0) return;

            // đổ dữ liệu Get_Subscribes vào combobox
            LayDSPM("SELECT * FROM Get_Subscribes");

            cmbKhoa.DataSource = Program.bdsDSPM;
            cmbKhoa.DisplayMember = "TENPHONG";    // hiển thị: Khoa CNTT, Khoa VT, Kế Toán
            cmbKhoa.ValueMember = "TENSERVER";     // giá trị: ADMINISTRATOR\MSSQLSERVER3...
            cmbKhoa.SelectedIndex = 0;

            Program.servername = cmbKhoa.SelectedValue.ToString();
        }


        private void btnDangNhap_Click(object sender, EventArgs e)
        {
            if (txtUserName.Text.Trim() == "" || txtPass.Text.Trim() == "")
            {
               // MessageBox.Show("Tài khoản và mật khẩu không hợp lệ", "", MessageBoxButtons.OK);
                return;
            }

            // Lấy server từ combobox (an toàn)
            try
            {
                if (cmbKhoa.SelectedItem is DataRowView drv)
                    Program.servername = drv["TENSERVER"].ToString();
                else if (cmbKhoa.SelectedValue != null)
                    Program.servername = cmbKhoa.SelectedValue.ToString();
                else
                {
                   MessageBox.Show("Bạn chưa chọn Khoa (Phòng).", "Lỗi", MessageBoxButtons.OK);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lấy server từ combobox: " + ex.Message, "Lỗi", MessageBoxButtons.OK);
                return;
            }

            // --- Debug output: hiển thị server + user sẽ dùng để connect ---
            // Nếu bạn đang dùng Windows Auth, Program.mlogin/pass có thể rỗng — vẫn hiển thị để debug.
            string debugUser = string.IsNullOrEmpty(Program.mlogin) ? "(WindowsAuth / no SQL user)" : Program.mlogin;
            string dbgMsg = $"Đang thử kết nối tới site: {Program.servername}  |  Kết nối bằng user: {debugUser}";
            // Hiện MsgBox nhỏ để debug (bỏ/comment nếu phiền)
            MessageBox.Show(dbgMsg, "DEBUG: thử kết nối", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // Và in vào Output (Visual Studio -> Debug)
            System.Diagnostics.Debug.WriteLine(dbgMsg);

            // Nếu trước đó có gán Program.mlogin/Program.pass để dùng SQL Auth cho 1 số user, giữ nguyên.
            // Ở code gốc bạn reset Program.mlogin/pass rỗng để dùng Windows Auth — mình giữ logic đó:
            Program.mlogin = "";
            Program.pass = "";

            // Thực hiện kết nối Windows Auth tới server chọn
            if (Program.KetNoiWindowsAuth() == 0)
            {
                string failMsg = $"Không thể kết nối tới {Program.servername}. Vui lòng kiểm tra server / mạng / quyền.";
                //MessageBox.Show(failMsg, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine("KẾT NỐI THẤT BẠI: " + Program.servername);
                return;
            }
            else
            {
                string okMsg = $"Kết nối thành công tới {Program.servername}.";
                // hiển thị nhẹ, hoặc bạn có thể comment MessageBox nếu không muốn popup
                //MessageBox.Show(okMsg, "Kết nối OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Diagnostics.Debug.WriteLine("KẾT NỐI THÀNH CÔNG: " + Program.servername);
            }

            // --- Tiếp tục logic kiểm tra user như cũ ---
            String inputUsername = txtUserName.Text;
            String inputPassword = txtPass.Text;

            String strCmd = "";

            // Kiểm tra admin
            if (inputUsername.ToLower() == "sa" && inputPassword == "sa")
            {
                strCmd = "SELECT 'dbo' as USERNAME, 'System Administrator' as HOTEN, 'db_owner' as TENNHOM";
                Program.myReader = Program.ExecSqlDataReader(strCmd);
            }
            // Kiểm tra giáo viên
            else if (inputUsername.StartsWith("GV"))
            {
                strCmd = "SELECT '" + inputUsername + "' as USERNAME, HO + ' ' + TEN as HOTEN, 'GIANGVIEN' as TENNHOM FROM GIANGVIEN WHERE MAGV = '" + inputUsername + "'";
                Program.myReader = Program.ExecSqlDataReader(strCmd);

                if (Program.myReader != null && Program.myReader.HasRows == false)
                {
                    Program.myReader.Close();
                    MessageBox.Show("Mã giáo viên không tồn tại!", "Lỗi đăng nhập", MessageBoxButtons.OK);
                    return;
                }
            }
            // Kiểm tra kế toán
            else if (inputUsername.ToUpper() == "PKT01")
            {
                strCmd = "EXEC SP_CHECK_DANGNHAP '" + inputUsername + "'";
                Program.myReader = Program.ExecSqlDataReader(strCmd);

                if (Program.myReader != null && Program.myReader.HasRows == false)
                {
                    Program.myReader.Close();
                    MessageBox.Show("Tài khoản kế toán không tồn tại!", "Lỗi đăng nhập", MessageBoxButtons.OK);
                    return;
                }
            }
            // Kiểm tra sinh viên
            else
            {
                strCmd = "SELECT MASV as USERNAME, HO + ' ' + TEN as HOTEN, 'SINHVIEN' as TENNHOM FROM SINHVIEN WHERE MASV = '" + inputUsername + "' AND PASSWORD = '" + inputPassword + "'";
                Program.myReader = Program.ExecSqlDataReader(strCmd);

                if (Program.myReader != null && Program.myReader.HasRows == false)
                {
                    Program.myReader.Close();
                    MessageBox.Show("Mã sinh viên hoặc mật khẩu không đúng!", "Lỗi đăng nhập", MessageBoxButtons.OK);
                    return;
                }
            }

            Program.mPhongBan = cmbKhoa.SelectedIndex;
            Program.mloginDN = Program.mlogin;
            Program.passDN = Program.pass;
            if (Program.myReader == null) return;

            Program.myReader.Read();

            try
            {
                Program.username = Program.myReader.GetString(0);
                if (Convert.IsDBNull(Program.username))
                {
                    MessageBox.Show("Login không có quyền truy cập dữ liệu", "", MessageBoxButtons.OK);
                    return;
                }

                Program.mHoten = Program.myReader.IsDBNull(1) ? "" : Program.myReader.GetString(1);
                Program.mGroup = Program.myReader.IsDBNull(2) ? "" : Program.myReader.GetString(2);
                Program.myReader.Close();

                Program.frmChinh = new frmMain();
                Program.frmChinh.statusMa.Text = "MÃ: " + Program.username.ToUpper();
                Program.frmChinh.statusTen.Text = "TÊN: " + Program.mHoten;
                Program.frmChinh.statusKhoa.Text = "QUYỀN: " + Program.mGroup;
                this.Visible = false;
                Thread.Sleep(500);
                Program.frmChinh.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tài khoản hoặc mật khẩu không hợp lệ \n Vui long kiem tra lại \n" + ex.Message, "", MessageBoxButtons.OK);
                return;
            }
        }


        private void cmbKhoa_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Program.servername = cmbKhoa.SelectedValue.ToString();
            }
            catch (Exception)
            {

            }
        }
        public void loadAgain()
        {
            cmbKhoa.SelectedItem = Program.mGroup;
            Program.servername = cmbKhoa.SelectedValue.ToString();
            txtUserName.Text = null;
            txtPass.Text = null;
            txtUserName.Focus();

        }
    }
}