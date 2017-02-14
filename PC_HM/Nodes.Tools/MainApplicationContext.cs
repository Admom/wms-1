using System;
using System.ComponentModel;
using System.Windows.Forms;
using Nodes.DBHelper;
using Nodes.Entities;
using Nodes.Shares;
using Nodes.SystemManage;
using Nodes.Utils;
using Nodes.UI;
using Nodes.Stock;
using Nodes.Entities.HttpEntity;
using Nodes.Entities.HttpEntity.Stock;
using Newtonsoft.Json;

namespace Nodes.Tools
{
    class MainApplicationContext : ApplicationContext
    {
        #region ����

        BackgroundWorker loginWorker;
        FrmLogin frmLogin;
        string usercode = null;
        string password = null;
        bool shouldRun = true;
        bool hasException = false;
        Exception exception;
        UserEntity user = null;
        UserDal userDal = new UserDal();
        //UpdateUtil updateUtil = new UpdateUtil();
        string appId = "wms";

        #endregion

        #region ����

        /// <summary>
        /// Gets if the main application should run.
        /// </summary>
        public bool ShouldRun
        {
            get
            {
                return this.shouldRun;
            }
        }

        #endregion

        #region ���캯��

        public MainApplicationContext()
        {
            loginWorker = new System.ComponentModel.BackgroundWorker();
            loginWorker.DoWork += OnDoWork;
            loginWorker.RunWorkerCompleted += OnWorkerCompleted;

            RunInstance();
        }

        #endregion

        void RunInstance()
        {
            frmLogin = new FrmLogin();
            frmLogin.LoginEvent += DoClickEvent;
            if (DialogResult.OK == frmLogin.ShowDialog())
            {
                // �жϵ�ǰ�Ƿ��и���
                //if (updateUtil.HasUpdate(appId))
                //{
                //    // ���ǿ�Ƹ��£�����ʾ�û�ֱ�Ӹ���
                //    if (!string.IsNullOrEmpty(updateUtil.LocalVersion.WH_CODE) && 
                //        updateUtil.LocalVersion.WH_CODE.IndexOf(user.WarehouseCode) > -1 && 
                //        ((updateUtil.LocalVersion != null && updateUtil.LocalVersion.UPDATE_FLAG == 1) ||
                //        MsgBox.AskOK("ϵͳ��ǰ�и��£��Ƿ����ϵͳ��") == DialogResult.OK))
                //    {
                //        updateUtil.UpdateNow();
                //        this.shouldRun = false;
                //        this.ExitThread();
                //        return;
                //    }
                //}
                FrmToolMain frmMain = new FrmToolMain();
                frmMain.FormClosed += OmMainFormClosed;

                frmMain.Show();
                frmMain.Activate();
            }
            else
            {
                this.shouldRun = false;
                this.ExitThread();
            }
        }

        /// <summary>
        /// ��ȡһ���û�����ϸ��Ϣ
        /// </summary>
        /// <param name="USER_ID"></param>
        /// <returns></returns>
        public UserEntity GetUserInfo(string userCode)
        {
            UserEntity list = new UserEntity();
            try
            {
                #region ��������
                System.Text.StringBuilder loStr = new System.Text.StringBuilder();
                loStr.Append("userCode=").Append(userCode);
                string jsonQuery = WebWork.SendRequest(loStr.ToString(), WebWork.URL_GetUserInfo);
                if (string.IsNullOrEmpty(jsonQuery))
                {
                    MsgBox.Warn(WebWork.RESULT_NULL);
                    //LogHelper.InfoLog(WebWork.RESULT_NULL);
                    return list;
                }
                #endregion

                #region ����������

                JsonGetUserInfo bill = JsonConvert.DeserializeObject<JsonGetUserInfo>(jsonQuery);
                if (bill == null)
                {
                    MsgBox.Warn(WebWork.JSON_DATA_NULL);
                    return list;
                }
                if (bill.flag != 0)
                {
                    MsgBox.Warn(bill.error);
                    return list;
                }
                #endregion
                #region ��ֵ

                foreach (JsonGetUserInfoResult tm in bill.result)
                {
                    #region 0-10
                    list.AllowEdit = tm.allowEdit;
                    list.BranchCode = tm.branchCode;
                    list.CenterWarehouseCode = tm.centerWhCode;
                    list.IsActive = tm.isActive;
                    list.IsCenter = Convert.ToInt32(tm.isCenterWh);
                    list.IsOwn = tm.isOwn;
                    list.MobilePhone = tm.mobilePhone;
                    list.UserPwd = tm.pwd;
                    list.Remark = tm.remark;
                    list.LastUpdateBy = tm.updateBy;
                    #endregion

                    #region 11-15
                    list.UserCode = tm.userCode;
                    list.UserName = tm.userName;
                    list.WarehouseCode = tm.whCode;
                    list.WarehouseName = tm.whName;
                    #endregion

                    if (!string.IsNullOrEmpty(tm.updateDate))
                        list.LastUpdateDate = Convert.ToDateTime(tm.updateDate);
                }
                return list;
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
                //MsgBox.Err(ex.Message);
            }
            return list;
        }

        private void OnDoWork(object sender, DoWorkEventArgs e)
        {
            hasException = false;
            user = null;

            try
            {
                user = GetUserInfo(usercode);
            }
            catch (Exception ex)
            {
                hasException = true;
                exception = ex;
            }
        }

        private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            frmLogin.SetEnable(true);

            if (user == null)
            {
                if (hasException)
                    MsgBox.Warn(exception.Message);
                else
                    MsgBox.Warn("�ʺŲ����ڡ�");
            }
            else if (user.UserPwd != password)
            {
                MsgBox.Warn("�������");
            }
            else if (user != null)
            {
                //�����Ƿ�ע����¼
                if (user.IsActive == "N")
                    MsgBox.Warn("���ʺ��ѱ����ã��޷���¼��");
                else
                {
                    user.IPAddress = IPUtil.GetLocalIP();
                    GlobeSettings.LoginedUser = user;
                    
                    //LoginLogEntiy LoginLog = new LoginLogEntiy();
                    //LoginLog.UserCode = user.UserCode;
                    //LoginLog.IP = user.IPAddress;
                    //LoginLog.LoginDate = System.DateTime.Now;
                    //LoginLog.LoginType = "��¼";
                    //userDal.InsertLoginLog(LoginLog);

                    //��¼�ɹ��󣬼�ס�û���������
                    frmLogin.SaveMe();

                    frmLogin.DialogResult = DialogResult.OK;
                }
            }
        }

        private void DoClickEvent(object sender, EventArgs e)
        {
            password = SecurityUtil.MD5Encrypt(frmLogin.Password);
            usercode = frmLogin.User;

            loginWorker.RunWorkerAsync();
        }

        private void OmMainFormClosed(object sender, System.EventArgs e)
        {
            //userDal.InsertLoginLog(
            //    new LoginLogEntiy()
            //    {
            //        IP = user.IPAddress,
            //        UserCode = user.UserCode,
            //        LoginDate = System.DateTime.Now,
            //        LoginType = "�˳�"
            //    });

            this.ExitThread();
        }
    }
}