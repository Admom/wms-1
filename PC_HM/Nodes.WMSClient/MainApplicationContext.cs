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
using Nodes.Net;
using System.Text;
using Nodes.Entities.HttpEntity;
using Nodes.Entities.HttpEntity.Stock;
using Newtonsoft.Json;

namespace Nodes.WMSClient
{
    class MainApplicationContext : ApplicationContext
    {
        #region ����

        BackgroundWorker loginWorker;
        FrmLogin frmLogin;
        string usercode = null;
        string password = null;
        string pwd = null;
        bool shouldRun = true;
        bool hasException = false;
        Exception exception;
        UserEntity user = null;
        UserDal userDal = new UserDal();
        UpdateUtil updateUtil = new UpdateUtil();
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
                if (updateUtil.HasUpdate(appId))
                {
                    // ���ǿ�Ƹ��£�����ʾ�û�ֱ�Ӹ���
                    if (!string.IsNullOrEmpty(updateUtil.LocalVersion.WH_CODE) && 
                        updateUtil.LocalVersion.WH_CODE.IndexOf(user.WarehouseCode) > -1 && 
                        ((updateUtil.LocalVersion != null && updateUtil.LocalVersion.UPDATE_FLAG == 1) ||
                        MsgBox.AskOK("ϵͳ��ǰ�и��£��Ƿ����ϵͳ��") == DialogResult.OK))
                    {
                        updateUtil.UpdateNow();
                        this.shouldRun = false;
                        this.ExitThread();
                        return;
                    }
                }
                GlobeSettings.LocalVersion = updateUtil.LocalVersion;
#if !DEBUG
                //���ر�������
                FrmStockWarm warm = new FrmStockWarm();
                warm.ShowDialog();
#endif
                FrmMain frmMain = new FrmMain();
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

                WebWork.USER_CODE = user.UserCode;
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

            #region
            //#region ͬ��erp��¼�ӿ�
            //System.Text.StringBuilder loStr = new System.Text.StringBuilder();
            //loStr.Append("userCode=").Append(usercode).Append("&");
            //loStr.Append("passWord=").Append(pwd).Append("&");
            //loStr.Append("local=").Append(WebWork.URL_ADDRESS);
            //string loData = WebWork.SendRequest(loStr.ToString(), WebWork.USER_LOGIN);
            //if (string.IsNullOrEmpty(loData))
            //{
            //    MsgBox.Warn("û�л�ȡ�����ݣ�");
            //    return;
            //}

            //if (loData.ToUpper().Equals("NETWORK_EXCEPTION"))
            //{
            //    MsgBox.Warn("����ʧ�ܣ������쳣��");
            //    return;
            //}

            //JsonLogin loLoginMsg = JsonConvert.DeserializeObject<JsonLogin>(loData);

            //if (loLoginMsg == null)
            //{
            //    MsgBox.Warn("�û���Ų����ڡ�");
            //    return;
            //}
            //if (loLoginMsg.flag != 0)
            //{
            //    MsgBox.Warn(loLoginMsg.error);
            //    return;
            //}
            //if (loLoginMsg.result.Length <= 0)
            //{
            //    MsgBox.Warn("û�в�ѯ�����û���Ϣ");
            //    return;
            //}
            //user = new UserEntity();
            //user.UserCode = loLoginMsg.result[0].userCode;
            //user.UserName = loLoginMsg.result[0].userName;
            //user.UserPwd = pwd;
            //user.IsActive = loLoginMsg.result[0].isActive;
            //user.WarehouseCode = loLoginMsg.result[0].wh_code;
            //user.WarehouseName = loLoginMsg.result[0].wh_name;
            //user.IsCenter = loLoginMsg.result[0].is_center_wh;
            //user.whType = loLoginMsg.result[0].wareHouseType;
            //user.CenterWarehouseCode = loLoginMsg.result[0].center_wh_code;
            //#endregion
            //WebWork.USER_CODE = user.UserCode;

            //user.IPAddress = IPUtil.GetLocalIP();
            //GlobeSettings.LoginedUser = user;

            ////��¼�ɹ��󣬼�ס�û���������
            //frmLogin.SaveMe();

            //frmLogin.DialogResult = DialogResult.OK;
            #endregion

            #region ԭ�ȵ�¼�ӿ�
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
            #endregion
        }

        private void DoClickEvent(object sender, EventArgs e)
        {
            password = SecurityUtil.MD5Encrypt(frmLogin.Password);
            pwd = frmLogin.Password;
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