   M S B A M L   `   `   `  ����D  @PowerTray, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:PowerTray	PowerTray  5   	       PowerTray.Settings   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   )localclr-namespace:PowerTray     5   	   A =Wpf.Ui, Version=3.0.4.0, Culture=neutral, PublicKeyToken=null/ui&http://schemas.lepo.co/wpfui/2022/xaml  5   	     � Title$  PowerTray Settings��5
   	   $	��620��6$   $	��420��61   $	��500��6=   $	��350��6M   "  Wpf.Ui.Controls.FluentWindow!   ExtendsContentIntoTitleBar$
 True��5   	      WindowBackdropType$
 Mica=�5   	      WindowCornerPreference$ Round=�5   	    � WindowStartupLocation$ CenterScreen=�5   	   .��5      � -   5         �Name$ Window��5       � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
     Wpf.Ui.Controls.TitleBar     Title$  ��5      $��0��5      $��30��5         Icon*  $Wpf.Ui.Controls.IconElementConverter$; 5pack://application:,,,/Assets/applicationIcon-256.png 5      �� 5   
   $��20(�6   $
��Bold"�6"   $��0��64   $��	10,5,0,10q�6A   $��PowerTray Settings��6T   6p   )  #Wpf.Ui.Controls.DynamicScrollViewer  5   
   $��1��6!   $
��Auto=�6.   $
��Auto=�6Q   .��5       � .+�5!      �� $��	10,0,10,0q�6   $��Stretch=�60   	 �� Orientation$	 Vertical=�6N   .+�5"      "  Wpf.Ui.Controls.CardExpander  $��General Settings��6&   
 9� 
IsExpanded$

 True��6@   .��5#      �� $��-5q�6%   $��Stretch=�61   $	 Vertical=�6O   .+�5$      � $��0q�6#    � ToolTip$: 4This puts the PowerTray windows on top of all others��6.    � ColumnDefinitions 5%   "   �� 5&   &   $��* �67   �� 5'   &   $
��Auto �67   .+�5)   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Always on Top6u   "  Wpf.Ui.Controls.ToggleSwitch  5*   "   $��1��62   $��35��6B    d� 	IsChecked 6N   �� *AlwaysOnTop+6r   5+      � 5,      $��0,10,0,0q�6#   $< 6This option does nothing when Admin Startup is enabled��65    5-   "   �� 5.   &   $��* �67   �� 5/   &   $
��Auto �67   .+�51   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Run on Startup6v     52   "   $��1��62   $��35��6B    6N   �� *	Startup+6n   53      � 54      $��0,10,0,0q�6#   $M GRunning the program as Administrator is required to change this setting��65    55   "   �� 56   &   $��* �67   �� 57   &   $
��Auto �67   .+�59   "   �� -   5         ��Name$ 
AdminLabel��59   (   $��0��6<   $��35��6L   $��Center=�6X   .��6z   !Run as Administrator on Startup6�     5:   "   -   5          Name$ Admin��5:   2   $��1��6A   $��35��6Q    6]   �� *AdminStartup+6�   5;      � 5<      $��0,10,0,0q�6#   $A ;Values that are too large could cause the tray text to clip��65    5=   "   �� 5>   &   $��* �67   �� 5?   &   $
��Auto �67   .+�5A   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Tray Font Size6v     Wpf.Ui.Controls.TextBox  5B   "   $��1��6-   $	��120��6=   $��35��6I      ClearButtonEnabled$ False��6U   ��6p   �� *
FontSize+6�   5C      � 5D      $��0,10,0,0q�6#   $8 2The default information displayed on the tray icon��65    5E   "   �� 5F   &   $��* �67   �� 5G   &   $
��Auto �67   .+�5I   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Default Tray View6y   �� 5J   "   -   5         ��Name$ DefaultTray��5J   ;   $��1��6+    �� Text 6R   �� *
TrayText+6n   5K      5L      5M        5N      $��Power Plans��6&   $
 False��6;   $��0,5,0,0q�6N   .��5O      �� $��-5q�6%   $��Stretch=�61   $	 Vertical=�6O   .+�5P      � $��0q�6#    5Q   "   �� 5R   &   $��* �67   .+�5T   "     Wpf.Ui.Controls.Badge  -   5          Name$
 Info��5T   +   $��1��69   $��Stretch=�6I   $��Center=�6n   $��30��6�   .��6�   -+ⓘ Hover over buttons for more information6�   5U      � 5V      $��0,10,0,0q�6#    5W   "   �� 5X   &   $��* �67   .+�5[   "   �� -   5         ��Name$ Reset��5[   )   $� �This option adds back the default power plans if you don't have them (Modern Standby blocks all non-Balanced profiles). Here are the descriptions of the power plans:

- Balanced: Allows the CPU to have dynamic clock speeds, only using what it needs

- Power saver: Saves power. Consider using BatteryBoost instead for more battery life

- High Performance: Forces the CPU to use the highest clock speed continuously (for P-States)��68   $��0��5\   ?   $��Stretch=�6O   $��Stretch=�6m   $��35��6�   .��5]   &   � .+�5^   *    	  Wpf.Ui.Controls.SymbolIcon	  $
��Left=�68    	  Symbol$ Wrench20=�6S   
  Wpf.Ui.Controls.FontIcon 
  FontSize$ 24(�6e   �� 5_   *   $��Center=�60   .��6T   (&Repair and restore default power plans6|   5`   '   5a   #   5b      � 5c      $��0,10,0,0q�6#    5d   "   �� 5e   &   $��* �67   .+�5g   "   �� -   5        $ Boost��5g   )   $y sThis option adds BatteryBoost; a custom power plan that can improve battery life, especially when using USB devices��6S   $��0��6�   $��Stretch=�6�   $��Stretch=�6�   $��35��6$  .��5h   &   � .+�5i   *   	  $
��Left=�68   $ BatterySaver20=�6S   $ 24(�6k   �� 5j   *   $��Center=�60   .��6T   Add BatteryBoost profile6n   5k   '   5l   #   5m      � 5n      $��0,10,0,0q�6#    5o   "   �� 5p   &   $��* �67   .+�5r   "   �� -   5        $ Advanced��5r   )   $] WThis option unlocks hidden power plan settings (Admin required) (not easily reversible)��6R   $��0��6�   $��Stretch=�6�   $��Stretch=�6�   $��35��6  .��5s   &   � .+�5t   *   	  $
��Left=�68   $ Key20=�6S   $ 24(�6b   �� 5u   *   $��Center=�60   .��6T   %#Unlock Advanced Power Plan Settings6y   5v   '   5w   #   5x      � 5y      $��0,10,0,0q�6#    5z   "   �� 5{   &   $��* �67   �� 5|   &   $
��Auto �67   .+�5~   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Automatically Switch Plans6�   �� 5   "   -	   5         ��Name$
 Auto��5   +   $��1��69   $��35��6I    6n   �� *
AutoSwitch+6�   5�      � 5�      $��0,10,0,0q�6#    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� -
   5        $ 
NotifLabel��5�   D   $��0��6(   $��35��68   $��Center=�6X   {�6z   �� *
AutoSwitch+.��6�   Show Notifications6�     5�   "   -   5        $ Notif��5�   2   $��1��6A   $��35��6Q   {�6]   �� *
AutoSwitch+ 6~   �� *Notifs+6�   5�      � 5�      $��0,10,0,0q�6#    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� -   5        $ ACPlanLabel��5�   8   $��0��6(   $��35��6M   $��Center=�6Y   {�6{   �� *
AutoSwitch+.��6�   Default AC Power Plan6�   �� 5�   "   -   5        $ ACPlan��5�   ;   $��1��6+   $	��180��6�    6M   �� *ACPlan+{�6e   �� *
AutoSwitch+6�   5�      � 5�      $��0,10,0,0q�6#    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� -   5        $ BatteryPlanLabel��5�   8   $��0��6(   $��35��6R   $��Center=�6^   {�6�   �� *
AutoSwitch+.��6�   Default Battery Power Plan6�   �� 5�   "   -   5        $ BatteryPlan��5�   ;   $��1��6+   $	��180��6�    6R   �� *BatteryPlan+{�6o   �� *
AutoSwitch+6�   5�      5�      5�        5�      $��Buffers��6&   $
 False��67   $��0,5,0,0q�6J   .��5�      �� $��-5q�6%   $��Stretch=�61   $	 Vertical=�6O   .+�5�      � $��0q�6#   $d ^The size of the buffer for the calculated charge statistic (it is averaged over this interval)��6.    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   %#Calculated Charge Rate Buffer (sec)6�     5�   "   $��1��6-   $	��120��6=   $��35��6I   $ False��6U   ��6p   �� *
BufferSize+6�   5�      � 5�      $��0,10,0,0q�6#   $) #The amount of history for the graph��65    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Graphs History Length (sec)6�     5�   "   $��1��6-   $	��120��6=   $��35��6I   $ False��6U   ��6p   �� *HistoryLength+6�   5�      5�      5�        5�      $��Refresh Rates��6&   $
 False��6=   $��0,5,0,0q�6P   .��5�      �� $��-5q�6%   $��Stretch=�61   $	 Vertical=�6O   .+�5�      � $��0q�6#    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Tray Info Refresh Rate (ms)6�     5�   "   $��1��6-   $	��120��6=   $��35��6I   $ False��6U   ��6p   �� *TrayRefreshRate+6�   5�      � 5�      $��0,10,0,0q�6#    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Graphs Refresh Rate (ms)6�     5�   "   $��1��6-   $	��120��6=   $��35��6I   $ False��6U   ��6p   �� *GraphRefreshRate+6�   5�      � 5�      $��0,10,0,0q�6#    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f    Battery Info Refresh Rate (ms)6�     5�   "   $��1��6-   $	��120��6=   $��35��6I   $ False��6U   ��6p   �� *BatInfoRefreshRate+6�   5�      5�      5�        5�      $��Other��6&   $
 False��65   $��0,5,0,0q�6H   .��5�      �� $��-5q�6%   $��Stretch=�61   $	 Vertical=�6O   .+�5�      � $��0q�6#    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Tray Font Style6w   �� 5�   "   -   5        $ TrayFontStyle��5�   ;   $��1��6+   $	��120��6o    6T   �� *	FontStyle+6}   5�      � 5�      $��0,10,0,0q�6#   $_ YThe amount of battery charge considered to be medium (changes the tray icon to be orange)��65    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Medium Battery Charge6}     5�   "   $��1��6-   $	��120��6=   $��35��6I   $ False��6U   ��6p   �� *MediumCharge+6�   5�      � 5�      $��0,10,0,0q�6#   $Y SThe amount of battery charge considered to be low (changes the tray icon to be red)��65    5�   "   �� 5�   &   $��* �67   �� 5�   &   $
��Auto �67   .+�5�   "   �� $��0��6(   $��35��68   $��Center=�6D   .��6f   Low Battery Charge6z     5�   "   $��1��6-   $	��120��6=   $��35��6I   $ False��6U   ��6p   �� *	LowCharge+6�   5�      5�      5�      5�      5     5     � 5  
   -   5        $ BottomControls��5     $��2��6'   $��0,8,0,8q�65    5     �� 5     $��1* �6#   �� 5     $��1* �6#   �� 5     $��1* �6#   .+�5     �� -   5        $ 
SaveButton��5     $��0��6)   $	��100��69   $��Center=�6F   .��6|   Apply6�   �� 5     -   5        $ ResetButton��5     $��1��6*   $	��100��6:   $��Center=�6G   .��6~   Reset6�   �� 5      -   5        $ CloseButton��5      $��2��6*   $	��100��6:   $��Center=�6G   .��6~   Close6�   5!     5"     5#     