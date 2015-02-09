# -*- coding: utf-8 -*-
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import Select
from selenium.common.exceptions import NoSuchElementException
from selenium.common.exceptions import NoAlertPresentException
import unittest, time, re
import socket, sys
import traceback

Host = socket.gethostbyname(socket.gethostname())
Port = 12345

dut_ip = ''
account = ''
password = ''
# 讀取檔案方法一
i = 0
f = open('web_ip.txt', 'r')
for line in f:
    i = i + 1
    if i == 1:
        dut_ip = line.strip()
    elif i == 2:
        account = line.strip()
    elif i == 3:
        password = line.strip()
#f.close()
if account == '':
    account = 'admin'
print "http://"+account+":"+password+"@"+dut_ip

class AdvancedSettings(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        self.driver.set_page_load_timeout(5)
        self.driver.maximize_window()
        self.driver.implicitly_wait(30)
        self.base_url = "http://"+account+":"+password+"@"+dut_ip
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_advanced_settings(self):
        driver = self.driver
        # Serial port 1 advanced settings
        driver.get(self.base_url + "/advance_set.asp?port=1")
        # 讀取檔案方法二: http://www.codedata.com.tw/python/python-tutorial-the-1st-class-4-unicode-support-basic-input-output/
        for line in open('advanced_settings', 'r'):
            cmd = line.lower().split()
            cmd[1] = int(cmd[1])
            #if not '//' in cmd[0]:
            print cmd
            if cmd[0] == 'a':
                # TCP Timeout
                ## checkbox[Enable]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='cktcp_timeout']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cktcp_timeout']").click()
                    if 1 <= int(cmd[2]) <= 65535:
                        ### '(1~65535) seconds'
                        driver.find_element_by_name("tcp_time_out").clear()
                        driver.find_element_by_name("tcp_time_out").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='cktcp_timeout']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cktcp_timeout']").click()
            elif cmd[0] == 'b':
                # Keep Alive
                ## checkbox[Enable]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='cktcp_keep_alive']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cktcp_keep_alive']").click()
                    if 1 <= int(cmd[2]) <= 65535:
                        ### (1~65535) seconds
                        driver.find_element_by_name("tcp_keep_alive").clear()
                        driver.find_element_by_name("tcp_keep_alive").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='cktcp_keep_alive']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cktcp_keep_alive']").click()
            elif cmd[0] == 'c':
                # Serial to Network Packet Delimiter
                ## checkbox[Interval timeout]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='ckuart_time_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckuart_time_delimiter']").click()
                    if cmd[2].upper() == 'A':
                        ### radio[Auto(calculate by baudrate)]
                        if not driver.find_element_by_xpath("//input[@name='autoset']").is_selected():
                            driver.find_element_by_xpath("//input[@name='autoset']").click()
                    elif cmd[2].upper() == 'M':
                        ### radio[Manual setting]
                        if not driver.find_element_by_xpath("(//input[@name='autoset'])[2]").is_selected():
                            driver.find_element_by_xpath("(//input[@name='autoset'])[2]").click()
                            if 1 <= int(cmd[6]) <= 30000:
                                #### (1~30000) ms
                                driver.find_element_by_name("txt_uart_time").clear()
                                driver.find_element_by_name("txt_uart_time").send_keys(cmd[6])
                    ### checkbox[Discard Bytes]
                    if int(cmd[3]) == 1:
                        if not driver.find_element_by_xpath("//input[@name='discard_bytes']").is_selected():
                            driver.find_element_by_xpath("//input[@name='discard_bytes']").click()
                        Select(driver.find_element_by_name("discard_type")).select_by_visible_text(cmd[4])
                        if 1 <= int(cmd[5]) <= 1024:
                            ### 1~1024 bytes
                            driver.find_element_by_name("discard_length").clear()
                            driver.find_element_by_name("discard_length").send_keys(cmd[5])
                    elif int(cmd[3]) == 0:
                        if driver.find_element_by_xpath("//input[@name='discard_bytes']").is_selected():
                            driver.find_element_by_xpath("//input[@name='discard_bytes']").click()
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='ckuart_time_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckuart_time_delimiter']").click()
            elif cmd[0] == 'd':
                ## checkbox[Max. Bytes]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='ckuart_maxbt_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckuart_maxbt_delimiter']").click()
                    if 1 <= int(cmd[2]) <= 1452:
                        ### 1~1452 bytes
                        driver.find_element_by_name("txt_uart_maxbt").clear()
                        driver.find_element_by_name("txt_uart_maxbt").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='ckuart_maxbt_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckuart_maxbt_delimiter']").click()
            elif cmd[0] == 'e':
                ## checkbox[Character ]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='ckuart_char_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckuart_char_delimiter']").click()
                    
                    ### "0x"+ASCII Code, Ex. 0x0d or 0x0d0a
                    driver.find_element_by_name("txt_uart_char").clear()
                    driver.find_element_by_name("txt_uart_char").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='ckuart_char_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckuart_char_delimiter']").click()
            elif cmd[0] == 'f':
                # Network to Serial Packet Delimiter
                ## [Interval timeout]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='cknet_time_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cknet_time_delimiter']").click()
                    if 1 <= int(cmd[2]) <= 30000:
                        ### 1~30000 ms
                        driver.find_element_by_name("txt_net_time").clear()
                        driver.find_element_by_name("txt_net_time").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='cknet_time_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cknet_time_delimiter']").click()
            elif cmd[0] == 'g':
                ## [Max. Bytes]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='cknet_maxbt_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cknet_maxbt_delimiter']").click()
                    if 1 <= int(cmd[2]) <= 1452:
                        ### 1~1452 bytes
                        driver.find_element_by_name("txt_net_maxbt").clear()
                        driver.find_element_by_name("txt_net_maxbt").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='cknet_maxbt_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cknet_maxbt_delimiter']").click()
            elif cmd[0] == 'h':
                ## [Character]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='cknet_char_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cknet_char_delimiter']").click()
                    
                    ### "0x"+ASCII Code, Ex. 0x0d or 0x0d0a
                    driver.find_element_by_name("txt_net_char").clear()
                    driver.find_element_by_name("txt_net_char").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='cknet_char_delimiter']").is_selected():
                        driver.find_element_by_xpath("//input[@name='cknet_char_delimiter']").click()
            elif cmd[0] == 'i':
                # Character send interval
                ## [Enable]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='send_interval_check']").is_selected():
                        driver.find_element_by_xpath("//input[@name='send_interval_check']").click()
                    if 1 <= int(cmd[2]) <= 1000:
                        ### 1~1000 ms
                        driver.find_element_by_name("send_interval").clear()
                        driver.find_element_by_name("send_interval").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='send_interval_check']").is_selected():
                        driver.find_element_by_xpath("//input[@name='send_interval_check']").click()
            elif cmd[0] == 'j':
                # Response interval timeout
                ## [Enable]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='ckTranscation']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckTranscation']").click()
                    if 1 <= int(cmd[2]) <= 60000:
                        ### 1~60000 ms
                        driver.find_element_by_name("tran_interval").clear()
                        driver.find_element_by_name("tran_interval").send_keys(cmd[2])
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='ckTranscation']").is_selected():
                        driver.find_element_by_xpath("//input[@name='ckTranscation']").click()
            elif cmd[0] == 'k':
                # Serial FIFO
                ## [Enable (Disabling this option at baud rates higher than 115200bps would result in data loss). ]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='disabl_fifo']").is_selected():
                        driver.find_element_by_xpath("//input[@name='disabl_fifo']").click()
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='disabl_fifo']").is_selected():
                        driver.find_element_by_xpath("//input[@name='disabl_fifo']").click()
            elif cmd[0] == 'l':
                # Serial Buffer
                ## [Empty serial buffer when a new TCP connection is established]
                if cmd[1] == 1:
                    if not driver.find_element_by_xpath("//input[@name='empty_com']").is_selected():
                        driver.find_element_by_xpath("//input[@name='empty_com']").click()
                elif cmd[1] == 0:
                    if driver.find_element_by_xpath("//input[@name='empty_com']").is_selected():
                        driver.find_element_by_xpath("//input[@name='empty_com']").click()
        
        driver.find_element_by_name("advance_app").click()
        driver.find_element_by_name("save").click()
        
    def is_element_present(self, how, what):
        try: self.driver.find_element(by=how, value=what)
        except NoSuchElementException, e: return False
        return True
    
    def is_alert_present(self):
        try: self.driver.switch_to_alert()
        except NoAlertPresentException, e: return False
        return True
    
    def close_alert_and_get_its_text(self):
        try:
            alert = self.driver.switch_to_alert()
            alert_text = alert.text
            if self.accept_next_alert:
                alert.accept()
            else:
                alert.dismiss()
            return alert_text
        finally: self.accept_next_alert = True
    
    def tearDown(self):
        self.driver.quit()
        self.assertEqual([], self.verificationErrors)

if __name__ == "__main__":
    unittest.main()
