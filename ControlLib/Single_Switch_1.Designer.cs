namespace ControlLib
{
    partial class Single_Switch_1
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.道岔定位ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.道岔反位ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.道岔空闲ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.道岔锁闭ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.道岔占用ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(150, 150);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.道岔定位ToolStripMenuItem,
            this.道岔反位ToolStripMenuItem,
            this.道岔空闲ToolStripMenuItem,
            this.道岔锁闭ToolStripMenuItem,
            this.道岔占用ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(125, 114);
            // 
            // 道岔定位ToolStripMenuItem
            // 
            this.道岔定位ToolStripMenuItem.Name = "道岔定位ToolStripMenuItem";
            this.道岔定位ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.道岔定位ToolStripMenuItem.Text = "道岔定位";
            // 
            // 道岔反位ToolStripMenuItem
            // 
            this.道岔反位ToolStripMenuItem.Name = "道岔反位ToolStripMenuItem";
            this.道岔反位ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.道岔反位ToolStripMenuItem.Text = "道岔反位";
            // 
            // 道岔空闲ToolStripMenuItem
            // 
            this.道岔空闲ToolStripMenuItem.Name = "道岔空闲ToolStripMenuItem";
            this.道岔空闲ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.道岔空闲ToolStripMenuItem.Text = "道岔空闲";
            // 
            // 道岔锁闭ToolStripMenuItem
            // 
            this.道岔锁闭ToolStripMenuItem.Name = "道岔锁闭ToolStripMenuItem";
            this.道岔锁闭ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.道岔锁闭ToolStripMenuItem.Text = "道岔锁闭";
            // 
            // 道岔占用ToolStripMenuItem
            // 
            this.道岔占用ToolStripMenuItem.Name = "道岔占用ToolStripMenuItem";
            this.道岔占用ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.道岔占用ToolStripMenuItem.Text = "道岔占用";
            // 
            // Daocha_1_1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pictureBox1);
            this.Name = "Daocha_1_1";
            this.Load += new System.EventHandler(this.Daocha_1_1_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Onpaint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Mouse_Down);
            this.MouseEnter += new System.EventHandler(this.MounseEnter);
            this.MouseLeave += new System.EventHandler(this.MounseLeave);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 道岔定位ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 道岔反位ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 道岔空闲ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 道岔锁闭ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 道岔占用ToolStripMenuItem;
    }
}
