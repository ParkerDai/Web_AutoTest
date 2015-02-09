# -*- coding: utf-8 -*-
'''
SE5404D  v4.12   Serial Server v4.43
'''
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

class SerialTcpserverRs232(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        self.driver.set_page_load_timeout(5)
        self.driver.maximize_window()
        self.driver.implicitly_wait(30)
        self.base_url = "http://"+account+":"+password+"@"+dut_ip
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_(self):
        # 網路保持連線
        #while True:
        # Create a TCP/IP socket
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            # Connect the socket to the port where the server is listening
            sock.connect((Host, Port))
            #print >>sys.stderr, 'connecting to %s port %s' % server_address
        except socket.error:
            print 'Tcp connect failed'
            traceback.print_exc()
            time.sleep(1)
            #continue
        '''
        # 持續接收參數
        while True:
            ori_data = sock.recv(1024).strip()
            if len(ori_data) > 0:
                data = ori_data.lower() # 接收到的內容一律轉換小寫!
                if data == 'exit':
                    break
                else:
                    data = data.split(' ')
                    if data[0] == 'test':
                        if len(data) != 2:
                            sock.send('cmderror')
                            continue
                        sock.send('ok')
                    elif data[0] == 'parker':
                        if len(data) != 2:
                            sock.send('cmd error')
                            continue
                        sock.send('parker respond')
                    elif data[0] == '':
                        seriel_settings(data)
                    else:
                        continue
        if data == 'exit':
            sock.close()
            break
        '''
        
        try:
            driver = self.driver
            # [Serial]
            driver.get(self.base_url + "/serial.asp")
            # [COM 1]
            # ERROR: Caught exception [ERROR: Unsupported command [selectFrame | left | ]]
            # switch_to
            driver.switch_to_frame("left")
            driver.find_element_by_link_text("COM 1").click()
            # switch_to
            # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rbottom | ]]
            driver.switch_to_default_content()
            driver.switch_to_frame("rbottom")
            # [TCP Server]
            driver.find_element_by_name("mode").click()
            driver.find_element_by_css_selector("strong > input[name=\"mode\"]").click()
            # ERROR: Caught exception [Error: Dom locators are not implemented yet!]
            driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/table/tbody/tr[2]/td/p/font/strong/input[2]").click()
            driver.find_element_by_name("mode").click()
            # [Mode]
            Select(driver.find_element_by_name("runmode")).select_by_visible_text("RAW")
            # [Max. Connections]
            Select(driver.find_element_by_name("max_conn")).select_by_visible_text("1")
            # [Response Behavior]
            #driver.find_element_by_name("tran_mode_server").click()
            driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[2]/table/tbody/tr[4]/td[2]/input[4]").click()
            # [Accessible IP]
            # uncheck!
            if driver.find_element_by_xpath("//div[2]/table/tbody/tr[5]/td[2]/input").is_selected():
                driver.find_element_by_xpath("//div[2]/table/tbody/tr[5]/td[2]/input").click()
            '''
            # check!
            if not driver.find_element_by_xpath("//div[2]/table/tbody/tr[5]/td[2]/input").is_selected():
                driver.find_element_by_xpath("//div[2]/table/tbody/tr[5]/td[2]/input").click()
                driver.find_element_by_name("txtIPFilter1").clear()
                driver.find_element_by_name("txtIPFilter1").send_keys("10")
                driver.find_element_by_name("txtIPFilter2").clear()
                driver.find_element_by_name("txtIPFilter2").send_keys("0")
                driver.find_element_by_name("txtIPFilter3").clear()
                driver.find_element_by_name("txtIPFilter3").send_keys("50")
                driver.find_element_by_name("txtIPFilter4").clear()
                driver.find_element_by_name("txtIPFilter4").send_keys("153")
            '''
            # [Local Port]
            # ERROR: Caught exception [Error: Dom locators are not implemented yet!]
            driver.find_element_by_name("localport").clear()
            driver.find_element_by_name("localport").send_keys("4660")
            # [Apply to all serial ports (Local Port will be enumerated automatically.)]
            driver.find_element_by_name("server_app").click()
            # [UART Mode]
            driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[2]/td[2]/input[3]").click()
            # [Baud Rate]
            Select(driver.find_element_by_id("id_cbbaudrate")).select_by_visible_text("115200")
            # [Parity]
            driver.find_element_by_name("rad_parity").click()
            # [Data bits]
            # ERROR: Caught exception [Error: Dom locators are not implemented yet!]
            driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[5]/td[2]/input[4]").click()
            # [Stop bits]
            driver.find_element_by_name("rad_stopbit").click()
            # [Flow Control]
            driver.find_element_by_name("rad_flow").click()
            # [Apply to all serial ports]
            driver.find_element_by_name("serial_app").click()
            # [Sace Configuration] Button
            driver.find_element_by_name("save").click()
        except:
            sock.send('set fail')
            print 'set fail'
        else:
            sock.send('set ok')
            print 'set ok'
        
        sock.close()
        
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
