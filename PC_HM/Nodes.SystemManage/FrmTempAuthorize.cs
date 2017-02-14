using System;
using System.Windows.Forms;
using Nodes.UI;
//using Nodes.DBHelper;
using Nodes.Utils;
using Nodes.Entities.HttpEntity;
using Nodes.Entities.HttpEntity.SystemManage;
using Newtonsoft.Json;

namespace Nodes.SystemManage
{
    public partial class FrmTempAuthorize : DevExpress.XtraEditors.XtraForm
    {
        private string RoleName;
        //private UserDal userDal;
        public string AuthUserCode = string.Empty;

        public FrmTempAuthorize(string roleName)
        {
            InitializeComponent();

            RoleName = roleName;
            //userDal = new UserDal();
            labelControl1.Text = string.Format("��ǰ������Ҫ�õ���ɫ��{0}������Ȩ��", roleName);
        }

        private void OnOKClick(object sender, EventArgs e)
        {
            OkPressed();
        }

        private void OnUserKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.IsEntryComplete())
                {
                    this.OkPressed();
                }
                else
                {
                    if (String.IsNullOrEmpty(this.txtID.Text.Trim()))
                    {
                        this.ShowUserRequired();
                    }
                    else
                    {
                        this.txtPwd.Focus();
                    }
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }

        }

        private void OnPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.IsEntryComplete())
                {
                    this.OkPressed();
                }
                else
                {
                    if (String.IsNullOrEmpty(this.txtPwd.Text.Trim()))
                    {
                        this.ShowPasswordRequired();
                    }
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ShowUserRequired()
        {
            if (!this.Disposing)
            {
                MsgBox.Warn("�����빤�š�����������ƴ����ͷ��ĸ��");
                this.txtID.Focus();
            }
        }

        /// <summary>
        /// �����û������Ϣ���鿴�Ƿ��ɴ�Ȩ��
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="pwd"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public string TempAuthorize(string userCode, string pwd, string roleName)
        {
            try
            {
                #region ��������
                System.Text.StringBuilder loStr = new System.Text.StringBuilder();
                loStr.Append("userCode=").Append(userCode).Append("&");
                loStr.Append("password=").Append(pwd).Append("&");
                loStr.Append("roleName=").Append(roleName);
                string jsonQuery = WebWork.SendRequest(loStr.ToString(), WebWork.URL_TempAuthorize);
                if (string.IsNullOrEmpty(jsonQuery))
                {
                    MsgBox.Warn(WebWork.RESULT_NULL);
                    //LogHelper.InfoLog(WebWork.RESULT_NULL);
                    return null;
                }
                #endregion

                #region ����������

                JsonTempAuthorize bill = JsonConvert.DeserializeObject<JsonTempAuthorize>(jsonQuery);
                if (bill == null)
                {
                    MsgBox.Warn(WebWork.JSON_DATA_NULL);
                    return null;
                }
                if (bill.flag != 0)
                {
                    MsgBox.Warn(bill.error);
                    return null;
                }
                #endregion
                if (bill.result != null && bill.result.Length > 0)
                    return bill.result[0].userCode;
            }
            catch (Exception ex)
            {
                MsgBox.Err(ex.Message);
            }

            return null;
        }

        private void OkPressed()
        {
            if (this.txtID.Text.Trim().Length == 0)
            {
                this.ShowUserRequired();
                return;
            }

            if (String.IsNullOrEmpty(this.txtPwd.Text.Trim()))
            {
                this.ShowPasswordRequired();
                return;
            }

            try
            {
                string ret = TempAuthorize(txtID.Text.Trim(), txtPwd.Text.Trim(), this.RoleName);
                if (!string.IsNullOrEmpty(ret))
                {
                    AuthUserCode = ret;
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MsgBox.Warn("���Ż��������󡢻���û��ѱ�ע������û��Ȩ�ޣ���ȷ�Ϲ����������������ϵ����Ա������ص�Ȩ�ޡ�");
                }
            }
            catch (Exception ex)
            {
                MsgBox.Err(ex.Message);
            }
        }

        /// <summary>
        /// Determines if based on the current configuration has everything that is required been filed in.
        /// </summary>
        /// <returns>true or false</returns>
        private bool IsEntryComplete()
        {
            bool returnValue = false;

            if (!String.IsNullOrEmpty(this.txtID.Text.Trim()) && !String.IsNullOrEmpty(this.txtPwd.Text.Trim()))
            {
                returnValue = true;
            }

            return returnValue;
        }

        public void ShowBadUserPassword()
        {
            if (!this.Disposing)
            {
                //this.Reset();

                this.txtPwd.Text = string.Empty;
                this.txtPwd.Focus();
                MsgBox.Warn("���Ż��������");
            }
        }

        private void ShowPasswordRequired()
        {
            if (!this.Disposing)
            {
                MsgBox.Warn("��������");
                this.txtPwd.Focus();
            }
        }
    }
}