namespace GUIChatClient {
    partial class Form1 {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.btReg = new System.Windows.Forms.Button();
            this.tbNick = new System.Windows.Forms.TextBox();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.tbRegResult = new System.Windows.Forms.TextBox();
            this.tbConv = new System.Windows.Forms.TextBox();
            this.btSend = new System.Windows.Forms.Button();
            this.tbMsg = new System.Windows.Forms.TextBox();
            this.lbPeerList = new System.Windows.Forms.Label();
            this.lbNick = new System.Windows.Forms.Label();
            this.lbPort = new System.Windows.Forms.Label();
            this.lbConv = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btReg
            // 
            this.btReg.Location = new System.Drawing.Point(10, 22);
            this.btReg.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btReg.Name = "btReg";
            this.btReg.Size = new System.Drawing.Size(74, 22);
            this.btReg.TabIndex = 0;
            this.btReg.Text = "Register";
            this.btReg.UseVisualStyleBackColor = true;
            this.btReg.Click += new System.EventHandler(this.btReg_Click);
            // 
            // tbNick
            // 
            this.tbNick.Location = new System.Drawing.Point(89, 22);
            this.tbNick.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbNick.Name = "tbNick";
            this.tbNick.Size = new System.Drawing.Size(120, 23);
            this.tbNick.TabIndex = 1;
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(214, 22);
            this.tbPort.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(95, 23);
            this.tbPort.TabIndex = 2;
            // 
            // tbRegResult
            // 
            this.tbRegResult.Location = new System.Drawing.Point(314, 22);
            this.tbRegResult.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbRegResult.Multiline = true;
            this.tbRegResult.Name = "tbRegResult";
            this.tbRegResult.Size = new System.Drawing.Size(134, 290);
            this.tbRegResult.TabIndex = 3;
            // 
            // tbConv
            // 
            this.tbConv.Location = new System.Drawing.Point(10, 103);
            this.tbConv.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbConv.Multiline = true;
            this.tbConv.Name = "tbConv";
            this.tbConv.Size = new System.Drawing.Size(299, 210);
            this.tbConv.TabIndex = 4;
            // 
            // btSend
            // 
            this.btSend.Location = new System.Drawing.Point(10, 55);
            this.btSend.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btSend.Name = "btSend";
            this.btSend.Size = new System.Drawing.Size(74, 22);
            this.btSend.TabIndex = 5;
            this.btSend.Text = "Send";
            this.btSend.UseVisualStyleBackColor = true;
            this.btSend.Click += new System.EventHandler(this.btSend_Click);
            // 
            // tbMsg
            // 
            this.tbMsg.Location = new System.Drawing.Point(89, 56);
            this.tbMsg.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbMsg.Name = "tbMsg";
            this.tbMsg.Size = new System.Drawing.Size(220, 23);
            this.tbMsg.TabIndex = 6;
            // 
            // lbPeerList
            // 
            this.lbPeerList.AutoSize = true;
            this.lbPeerList.Location = new System.Drawing.Point(314, 7);
            this.lbPeerList.Name = "lbPeerList";
            this.lbPeerList.Size = new System.Drawing.Size(117, 15);
            this.lbPeerList.TabIndex = 7;
            this.lbPeerList.Text = "Online Users @ login";
            // 
            // lbNick
            // 
            this.lbNick.AutoSize = true;
            this.lbNick.Location = new System.Drawing.Point(89, 4);
            this.lbNick.Name = "lbNick";
            this.lbNick.Size = new System.Drawing.Size(61, 15);
            this.lbNick.TabIndex = 8;
            this.lbNick.Text = "Nickname";
            // 
            // lbPort
            // 
            this.lbPort.AutoSize = true;
            this.lbPort.Location = new System.Drawing.Point(214, 4);
            this.lbPort.Name = "lbPort";
            this.lbPort.Size = new System.Drawing.Size(29, 15);
            this.lbPort.TabIndex = 9;
            this.lbPort.Text = "Port";
            // 
            // lbConv
            // 
            this.lbConv.AutoSize = true;
            this.lbConv.Location = new System.Drawing.Point(10, 86);
            this.lbConv.Name = "lbConv";
            this.lbConv.Size = new System.Drawing.Size(77, 15);
            this.lbConv.TabIndex = 10;
            this.lbConv.Text = "Conversation";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(458, 322);
            this.Controls.Add(this.lbConv);
            this.Controls.Add(this.lbPort);
            this.Controls.Add(this.lbNick);
            this.Controls.Add(this.lbPeerList);
            this.Controls.Add(this.tbMsg);
            this.Controls.Add(this.btSend);
            this.Controls.Add(this.tbConv);
            this.Controls.Add(this.tbRegResult);
            this.Controls.Add(this.tbPort);
            this.Controls.Add(this.tbNick);
            this.Controls.Add(this.btReg);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.form1_Closing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btReg;
        private System.Windows.Forms.TextBox tbNick;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.TextBox tbRegResult;
        private System.Windows.Forms.TextBox tbConv;
        private System.Windows.Forms.Button btSend;
        private System.Windows.Forms.TextBox tbMsg;
        private System.Windows.Forms.Label lbPeerList;
        private System.Windows.Forms.Label lbNick;
        private System.Windows.Forms.Label lbPort;
        private System.Windows.Forms.Label lbConv;
    }
}

