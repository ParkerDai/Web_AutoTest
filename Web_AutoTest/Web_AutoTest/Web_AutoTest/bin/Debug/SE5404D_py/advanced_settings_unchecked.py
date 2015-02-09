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
        
        # TCP Timeout
        ## checkbox[Enable]
        if driver.find_element_by_xpath("//input[@name='cktcp_timeout']").is_selected():
            driver.find_element_by_xpath("//input[@name='cktcp_timeout']").click()
            ### (1~65535) seconds
            #driver.find_element_by_name("tcp_time_out").clear()
            #driver.find_element_by_name("tcp_time_out").send_keys("3600")
        # Keep Alive
        ## checkbox[Enable]
        if driver.find_element_by_xpath("//input[@name='cktcp_keep_alive']").is_selected():
            driver.find_element_by_xpath("//input[@name='cktcp_keep_alive']").click()
            ### (1~65535) seconds
            #driver.find_element_by_name("tcp_keep_alive").clear()
            #driver.find_element_by_name("tcp_keep_alive").send_keys("10")
        # Serial to Network Packet Delimiter
        ## checkbox[Interval timeout]
        if driver.find_element_by_xpath("//input[@name='ckuart_time_delimiter']").is_selected():
            driver.find_element_by_xpath("//input[@name='ckuart_time_delimiter']").click()
            ### radio[Auto(calculate by baudrate)]
            '''
            if not driver.find_element_by_xpath("//input[@name='autoset']").is_selected():
                driver.find_element_by_xpath("//input[@name='autoset']").click()
            ### radio[Manual setting]
            if not driver.find_element_by_xpath("(//input[@name='autoset'])[2]").is_selected():
                driver.find_element_by_xpath("(//input[@name='autoset'])[2]").click()
                #### (1~30000) ms
                driver.find_element_by_name("txt_uart_time").clear()
                driver.find_element_by_name("txt_uart_time").send_keys("3")
            ### checkbox[Discard Bytes]
            if not driver.find_element_by_xpath("//input[@name='discard_bytes']").is_selected():
                driver.find_element_by_xpath("//input[@name='discard_bytes']").click()
                Select(driver.find_element_by_name("discard_type")).select_by_visible_text("<")
                driver.find_element_by_name("discard_length").clear()
                driver.find_element_by_name("discard_length").send_keys("3")
            '''
        ## checkbox[Max. Bytes]
        if driver.find_element_by_xpath("//input[@name='ckuart_maxbt_delimiter']").is_selected():
            driver.find_element_by_xpath("//input[@name='ckuart_maxbt_delimiter']").click()
            #driver.find_element_by_name("txt_uart_maxbt").clear()
            #driver.find_element_by_name("txt_uart_maxbt").send_keys("1452")
        ## checkbox[Character ]
        if driver.find_element_by_xpath("//input[@name='ckuart_char_delimiter']").is_selected():
            driver.find_element_by_xpath("//input[@name='ckuart_char_delimiter']").click()
            #driver.find_element_by_name("txt_uart_char").clear()
            #driver.find_element_by_name("txt_uart_char").send_keys("0x0d0a")
        # Network to Serial Packet Delimiter
        ## [Interval timeout]
        if driver.find_element_by_xpath("//input[@name='cknet_time_delimiter']").is_selected():
            driver.find_element_by_xpath("//input[@name='cknet_time_delimiter']").click()
            #driver.find_element_by_name("txt_net_time").clear()
            #driver.find_element_by_name("txt_net_time").send_keys("3")
        ## [Max. Bytes]
        if driver.find_element_by_xpath("//input[@name='cknet_maxbt_delimiter']").is_selected():
            driver.find_element_by_xpath("//input[@name='cknet_maxbt_delimiter']").click()
            #driver.find_element_by_name("txt_net_maxbt").clear()
            #driver.find_element_by_name("txt_net_maxbt").send_keys("1452")
        ## [Character]
        if driver.find_element_by_xpath("//input[@name='cknet_char_delimiter']").is_selected():
            driver.find_element_by_xpath("//input[@name='cknet_char_delimiter']").click()
            #driver.find_element_by_name("txt_net_char").clear()
            #driver.find_element_by_name("txt_net_char").send_keys("0x0d0a")
        # Character send interval
        ## [Enable]
        if driver.find_element_by_xpath("//input[@name='send_interval_check']").is_selected():
            driver.find_element_by_xpath("//input[@name='send_interval_check']").click()
            #driver.find_element_by_name("send_interval").clear()
            #driver.find_element_by_name("send_interval").send_keys("1000")
        # Response interval timeout
        ## [Enable]
        if driver.find_element_by_xpath("//input[@name='ckTranscation']").is_selected():
            driver.find_element_by_xpath("//input[@name='ckTranscation']").click()
            #driver.find_element_by_name("tran_interval").clear()
            #driver.find_element_by_name("tran_interval").send_keys("3600")
        # Serial FIFO
        ## [Enable (Disabling this option at baud rates higher than 115200bps would result in data loss). ]
        if driver.find_element_by_xpath("//input[@name='disabl_fifo']").is_selected():
            driver.find_element_by_xpath("//input[@name='disabl_fifo']").click()
        # Serial Buffer
        ## [Empty serial buffer when a new TCP connection is established]
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
