   M S B A M L   `   `   `  ����D  @PowerTray, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:PowerTray	PowerTray  5   	       PowerTray.BatInfo   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   )localclr-namespace:PowerTray     5   	   A =Wpf.Ui, Version=3.0.4.0, Culture=neutral, PublicKeyToken=null/ui&http://schemas.lepo.co/wpfui/2022/xaml  5   	     � Title$  Battery Info��5
   	   $	��480��6   $	��320��6+   $	��115��67   $	��300��6G    � Topmost$
 True��5   	   "  Wpf.Ui.Controls.FluentWindow!   ExtendsContentIntoTitleBar$
 True��5   	      WindowBackdropType$
 Mica=�5   	      WindowCornerPreference$ Round=�5   	    � WindowStartupLocation$ CenterScreen=�5   	   .��5      � -   5         �Name$ Window��5       � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
     Wpf.Ui.Controls.TitleBar     Title$ Battery Info��5      $��15(�5      $
��Bold"�5      $��0��5      $��30��5      	   Icon*  $Wpf.Ui.Controls.IconElementConverter$;	 5pack://application:,,,/Assets/applicationIcon-256.png 5      )  #Wpf.Ui.Controls.DynamicScrollViewer  5    
   $��1��6!   $
��Auto=�6.   $
��Auto=�6Q   .��5!      �� -   5        
 ��Name$

 Data��5!      $��1��6)   ��5"      �� 5#      .R�5$      ��  �� Orientation$ Vertical=�6%   5%      ��5'      �� 5(      .R�5)      �  � ColumnDefinitions 5*      �� 5+   "   $��* �63   �� 5,   "   $��* �63   .+�5.        Wpf.Ui.Controls.Badge  $��35��6'   $��10,5,5,5q�63      
Appearance$ Transparent=�6E   .��5/   "   �� $��Bottom=�6A   ��6(   �� *Name+6A   50        52      $��35��6'   $��5,5,10,5q�63   $��1��6E   $ Transparent=�6U   .��53   "   �� $��Bottom=�6B   $��Right=�6]   $
��Bold"�6y   ��6(   �� *Value+6y   54      55      56      58      59      � 5;   
   -   5        $ BottomControls��5;      $��2��6'   $��0,5,0,5q�65    5<      �� 5=      $��1* �6#   �� 5>      $��1* �6#   �� 5?      $��1* �6#   .+�5A      �� -   5         ��Name$ GraphButton��5A      $��0��6*   $	��100��6:   $��Center=�6G   .��6d   Graphs6l   �� 5B      -   5        $ ResetButton��5B      $��1��6*   $	��100��6:    � ToolTip$, &Clear the calculate charge rate buffer��6G   $��Center=�6y   .��6�   Clear Buffer6�   �� 5C      -   5        $ CloseButton��5C      $��2��6*   $	��100��6:   $��Center=�6G   .��6d   Close6k   5D      5F      5G      