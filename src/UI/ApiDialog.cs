using System;
using System.Drawing;
using System.Windows.Forms;
namespace JouchoPet
{
    sealed class ApiDialog : Form
    {
        public ApiSettings Value;
        public ApiDialog(ApiSettings current)
        {
            Text="Chat Completions API 设置";
            Size=new Size(640,350);
            StartPosition=FormStartPosition.CenterParent;
            Font=new Font("Microsoft YaHei UI",10);
            var layout=new TableLayoutPanel
            {
                Dock=DockStyle.Fill,ColumnCount=2,RowCount=6,Padding=new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            var endpoint=new TextBox
            {
                Text=current.Endpoint,Dock=DockStyle.Fill
            };
            var model=new TextBox
            {
                Text=current.Model,Dock=DockStyle.Fill
            };
            var key=new TextBox
            {
                Text=current.Key,UseSystemPasswordChar=true,Dock=DockStyle.Fill
            };
            var remember=new CheckBox
            {
                Text="在本机加密保存密钥（当前 Windows 用户）",Checked=current.Remember,AutoSize=true
            };
            layout.Controls.Add(new Label
            {
                Text="API 地址"
            },0,0);
            layout.Controls.Add(endpoint,1,0);
            layout.Controls.Add(new Label
            {
                Text="模型名"
            },0,1);
            layout.Controls.Add(model,1,1);
            layout.Controls.Add(new Label
            {
                Text="API Key"
            },0,2);
            layout.Controls.Add(key,1,2);
            layout.Controls.Add(remember,1,3);
            layout.Controls.Add(new Label
            {
                AutoSize=true,Text="地址可填写 https://api.openai.com/v1 或完整 /chat/completions 地址。\n模型名按服务商填写；本机服务可留空密钥。保存后发送一句话即可测试。"
            },1,4);
            var save=new Button
            {
                Text="保存",AutoSize=true
            };
            layout.Controls.Add(save,1,5);
            Controls.Add(layout);
            save.Click+=delegate
            {
                try
                {
                    ApiClient.CompletionUri(endpoint.Text);
                    if(string.IsNullOrWhiteSpace(model.Text))throw new ArgumentException("请填写模型名。");
                    Value=new ApiSettings
                    {
                        Endpoint=endpoint.Text.Trim(),Model=model.Text.Trim(),Key=key.Text.Trim(),Remember=remember.Checked
                    };
                    Value.Save();
                    DialogResult=DialogResult.OK;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(this,ex.Message);
                }
            };
        }
    }
}
