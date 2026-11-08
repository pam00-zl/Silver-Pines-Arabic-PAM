using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

// Native, accessible controls with a reference-inspired burgundy and gold theme.
public sealed class GoldButton : Button {
 public bool Primary;
 bool hover;
 public GoldButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;TabStop=true;BackColor=Color.FromArgb(16,5,7);}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaintBackground(PaintEventArgs e){using(var brush=new SolidBrush(BackColor))e.Graphics.FillRectangle(brush,ClientRectangle);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  var rect=new RectangleF(1,1,Width-3,Height-3);
  using(var path=InstallerForm.Round(rect,14)){
   Color fill=Primary?Color.FromArgb(231,185,100):Color.FromArgb(30,12,14);
   if(hover && Enabled)fill=Primary?Color.FromArgb(245,204,127):Color.FromArgb(49,23,24);
   if(!Enabled && Primary)fill=Color.FromArgb(133,104,61);
   using(var b=new SolidBrush(fill))g.FillPath(b,path);
   using(var pen=new Pen(Primary?Color.FromArgb(188,137,63):Color.FromArgb(146,119,108),1))g.DrawPath(pen,path);
  }
  var ink=Primary?Color.FromArgb(30,12,7):Color.FromArgb(244,222,190);
  if(!Enabled && !Primary)ink=Color.FromArgb(132,106,91);
  using(var brush=new SolidBrush(ink))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.DirectionRightToLeft|StringFormatFlags.NoWrap})g.DrawString(Text,Font,brush,ClientRectangle,format);
  if(Focused && ShowFocusCues)using(var pen=new Pen(ink,1)){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,Rectangle.Inflate(ClientRectangle,-7,-7));}
 }
}

public sealed class InstallerForm : Form {
 static readonly Color Gold=Color.FromArgb(230,184,103),Muted=Color.FromArgb(183,139,99);
 readonly Action<string,bool> apply;
 readonly Func<string,string> inspect;
 readonly PrivateFontCollection fonts=new PrivateFontCollection();
 readonly GoldButton install,remove;
 readonly LinkLabel browse;
 readonly Label found,gameName,pathLabel,state;
 readonly ToolTip tip=new ToolTip();
 readonly System.Windows.Forms.Timer spinner=new System.Windows.Forms.Timer();
 string root;
 bool busy;
 int revision;
 int tick;
 Font arabic;
 public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public InstallerForm(string initial,Action<string,bool> action,Func<string,string> checker){
  apply=action;inspect=checker;root=initial;
  AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);
  ClientSize=new Size(555,697);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
  Text="Silver Pines — التعريب العربي";BackColor=Color.FromArgb(16,5,7);DoubleBuffered=true;
  try{fonts.AddFontFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Amiri-Regular.ttf"));arabic=new Font(fonts.Families[0],18,FontStyle.Regular,GraphicsUnit.Pixel);}catch{arabic=new Font("Traditional Arabic",18,FontStyle.Regular,GraphicsUnit.Pixel);}
  Font=arabic;
  var credit=LabelAt("التعريب: P A M",7,28,20,Gold);
  credit.Bounds=new Rectangle(150,7,255,28);
  install=ButtonAt("تثبيت اللغة العربية",472,65,true,31);
  remove=ButtonAt("إزالة اللغة العربية",547,53,false,29);
  browse=new LinkLabel{Text="تحديد مجلد اللعبة يدويًا",AccessibleName="تحديد مجلد اللعبة يدويًا",Bounds=new Rectangle(37,646,481,34),Font=new Font(arabic.FontFamily,20,FontStyle.Regular,GraphicsUnit.Pixel),BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleCenter,RightToLeft=RightToLeft.Yes,UseCompatibleTextRendering=true,LinkBehavior=LinkBehavior.HoverUnderline,LinkColor=Muted,ActiveLinkColor=Gold,VisitedLinkColor=Muted};Controls.Add(browse);
  found=LabelAt("جارٍ البحث عن اللعبة…",383,25,20,Color.FromArgb(244,220,190));
  gameName=LabelAt("Silver Pines",411,25,18,Color.FromArgb(250,224,189));gameName.Font=new Font("Georgia",18,FontStyle.Regular,GraphicsUnit.Pixel);gameName.RightToLeft=RightToLeft.No;
  pathLabel=LabelAt("",439,19,12,Muted);pathLabel.Font=new Font("Segoe UI",11,FontStyle.Regular,GraphicsUnit.Pixel);pathLabel.RightToLeft=RightToLeft.No;pathLabel.AutoEllipsis=true;
  state=LabelAt("جارٍ التحقق من الملفات…",607,32,22,Gold);
  tip.SetToolTip(install,"يثبّت التعريب مع الاحتفاظ بالموارد الأصلية وملفات الحفظ.");tip.SetToolTip(remove,"يسترجع موارد اللعبة الأصلية من النسخ الاحتياطية.");
  browse.LinkClicked+=(s,e)=>{using(var dialog=new FolderBrowserDialog()){dialog.Description="اختر مجلد Silver Pines الذي يحتوي Carpenter.exe";if(Directory.Exists(root))dialog.SelectedPath=root;if(dialog.ShowDialog(this)==DialogResult.OK){root=dialog.SelectedPath;RefreshState();}}};
  install.Click+=async(s,e)=>await Run(false);remove.Click+=async(s,e)=>await Run(true);
  Shown+=(s,e)=>RefreshState();
  FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};
  spinner.Interval=400;spinner.Tick+=(s,e)=>{if(busy){tick=(tick+1)%4;state.Text="جارٍ تطبيق التغييرات"+new String('.',tick);}};
  TabIndex=0;install.TabIndex=0;remove.TabIndex=1;browse.TabIndex=2;
 }
 GoldButton ButtonAt(string text,int y,int height,bool primary,int size){var b=new GoldButton{Text=text,AccessibleName=text,Primary=primary,Bounds=new Rectangle(37,y,481,height),Font=new Font(arabic.FontFamily,size,FontStyle.Regular,GraphicsUnit.Pixel)};Controls.Add(b);return b;}
 protected override void OnLoad(EventArgs e){
  base.OnLoad(e);
  var area=Screen.FromControl(this).WorkingArea;
  float factor=Math.Min(1f,Math.Min((area.Height-28f)/Height,(area.Width-28f)/Width));
  if(factor<1f){
   var oldFonts=new System.Collections.Generic.Dictionary<Control,Font>();foreach(Control c in Controls)oldFonts[c]=c.Font;
   Scale(new SizeF(factor,factor));
   foreach(var pair in oldFonts)pair.Key.Font=new Font(pair.Value.FontFamily,pair.Value.Size*factor,pair.Value.Style,pair.Value.Unit);
   CenterToScreen();
  }
 }
 Label LabelAt(string text,int y,int height,int size,Color color){var l=new Label{Text=text,Bounds=new Rectangle(38,y,479,height),ForeColor=color,BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleCenter,RightToLeft=RightToLeft.Yes,Font=new Font(arabic.FontFamily,size,FontStyle.Regular,GraphicsUnit.Pixel),UseCompatibleTextRendering=true};Controls.Add(l);return l;}
 void RefreshState(){
  int request=++revision;install.Enabled=remove.Enabled=false;
  bool present=!String.IsNullOrEmpty(root)&&File.Exists(Path.Combine(root,"Carpenter.exe"));
  found.Text=present?"تم العثور على اللعبة":"لم يُعثر على اللعبة";
  pathLabel.Text=present?root:"اختر مجلد اللعبة للمتابعة";tip.SetToolTip(pathLabel,root);
  if(!present){state.Text="حدد مجلد اللعبة أولًا";return;}
  state.Text="جارٍ التحقق من إصدار اللعبة…";
  Task.Run(()=>{string result;try{result=inspect(root);}catch{result="unknown";}if(!IsDisposed && IsHandleCreated)BeginInvoke((Action)(()=>{if(request!=revision || busy)return;switch(result){
   case "installed":state.Text="✔ اللغة العربية مثبتة حاليًا";install.Enabled=remove.Enabled=true;break;
   case "original":state.Text="اللعبة جاهزة لتثبيت التعريب";install.Enabled=true;break;
   case "previous":state.Text="يتوفر تحديث للتعريب المثبت";install.Enabled=remove.Enabled=true;break;
   default:state.Text="إصدار مختلف أو ملفات غير مدعومة";break;
  }}));});
 }
 async Task Run(bool restore){
  if(busy)return;busy=true;++revision;install.Enabled=remove.Enabled=browse.Enabled=false;tick=0;spinner.Start();state.Text="جارٍ التحقق من الملفات…";
  Exception problem=null;
  try{await Task.Run(()=>apply(root,restore));}catch(Exception ex){problem=ex;}
  spinner.Stop();busy=false;browse.Enabled=true;RefreshState();
  if(problem!=null)MessageBox.Show(this,problem.Message+"\nإذا كان السبب رفض الكتابة، أغلق المثبت وشغله كمسؤول.","تعذر إتمام العملية",MessageBoxButtons.OK,MessageBoxIcon.Warning);
  else MessageBox.Show(this,restore?"تمت إزالة التعريب واسترجاع موارد اللعبة الأصلية. لم تتغير ملفات الحفظ.":"تم تثبيت التعريب. شغّل اللعبة واختر العربية في إعداد اللغة.","Silver Pines",MessageBoxButtons.OK,MessageBoxIcon.Information);
 }
 protected override void OnPaintBackground(PaintEventArgs e){
  var saved=e.Graphics.Save();try{
  var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var shape=new GraphicsPath()){shape.AddEllipse(-20,4,595,380);using(var glow=new PathGradientBrush(shape)){glow.CenterColor=Color.FromArgb(57,13,18);glow.SurroundColors=new[]{BackColor};g.FillPath(glow,shape);}}
  float sx=ClientSize.Width/555f,sy=ClientSize.Height/697f;g.ScaleTransform(sx,sy);
  using(var p=Round(new RectangleF(0.5f,0.5f,554,696),12))using(var pen=new Pen(Color.FromArgb(67,36,26),1))g.DrawPath(pen,p);
  using(var font=new Font("Segoe UI",17,FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(Muted))g.DrawString("v0.6.4",font,brush,53,7);
  using(var heading=new Font("Segoe UI",38,FontStyle.Bold,GraphicsUnit.Pixel))
   Center(g,"SILVER PINES",heading,64,72,Gold);
  using(var gold=new Pen(Color.FromArgb(145,100,63),1)){g.DrawLine(gold,137,172,418,172);g.DrawLine(gold,202,264,353,264);}
  using(var title=new Font(arabic.FontFamily,66,FontStyle.Regular,GraphicsUnit.Pixel))Center(g,"سيلفر باينز",title,175,86,Color.FromArgb(237,208,166));
  using(var title=new Font(arabic.FontFamily,40,FontStyle.Regular,GraphicsUnit.Pixel))Center(g,"التعريب العربي",title,272,55,Color.FromArgb(250,226,190));
  using(var sub=new Font(arabic.FontFamily,21,FontStyle.Regular,GraphicsUnit.Pixel))Center(g,"تعريب القصة والحوارات والقوائم",sub,330,29,Muted);
  using(var p=Round(new RectangleF(37,371,480,88),14)){
   using(var brush=new SolidBrush(Color.FromArgb(32,11,14)))g.FillPath(brush,p);
   using(var pen=new Pen(Color.FromArgb(108,69,37),1))g.DrawPath(pen,p);
  }
  }finally{e.Graphics.Restore(saved);}
 }
 void Center(Graphics g,string text,Font font,int y,int height,Color color){using(var brush=new SolidBrush(color))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.DirectionRightToLeft})g.DrawString(text,font,brush,new RectangleF(28,y,499,height),format);}
 protected override void Dispose(bool disposing){if(disposing){spinner.Dispose();tip.Dispose();fonts.Dispose();}base.Dispose(disposing);}
}
