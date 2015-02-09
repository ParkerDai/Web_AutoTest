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

model = None
kernel_ver = None
ap_ver = None
lan1_ip = None
lan1_mac = None
lan2_ip = None
lan2_mac = None
lan_max = -1

class Overview(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        self.driver.set_page_load_timeout(5)
        self.driver.maximize_window()
        self.driver.implicitly_wait(30)
        self.base_url = "http://"+account+":"+password+"@"+dut_ip
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_overview(self):
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        #sock.settimeout(10)
        try:
            sock.connect((Host, Port))
            sock.send('Connect ok'+"\r\n")
        except socket.error:
            print 'Connect failed'
            traceback.print_exc()
            time.sleep(1)

        try:
            driver = self.driver
            driver.get(self.base_url + "/index.asp")
            # ERROR: Caught exception [ERROR: Unsupported command [selectFrame | left | ]]
            driver.switch_to_frame("left")
            driver.find_element_by_name("Image1").click()
            # [Model]
            global model
            # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rtop | ]]
            driver.switch_to_default_content()
            driver.switch_to_frame("rtop")
            model = driver.find_element_by_css_selector("font.top_content").text
            # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rbottom | ]]
            driver.switch_to_default_content()
            driver.switch_to_frame("rbottom")
            # [Kernel]
            global kernel_ver
            kernel_ver = driver.find_element_by_css_selector("td.td_content_bg").text
            # [AP]
            global ap_ver
            ap_ver = driver.find_element_by_xpath("//tr[3]/td[2]").text
            # [Lan1 MAC]
            global lan1_mac
            lan1_mac = driver.find_element_by_xpath("//table[@id='eth']/tbody/tr[2]/td[3]").text
            # [Lan1 IP]
            global lan1_ip
            lan1_ip = driver.find_element_by_xpath("//table[@id='eth']/tbody/tr[3]/td[2]").text
            # [Lan2 MAC]
            global lan2_mac
            lan2_mac = driver.find_element_by_xpath("//table[@id='eth']/tbody/tr[4]/td[3]").text
            # [Lan2 IP]
            global lan2_ip
            lan2_ip = driver.find_element_by_xpath("//table[@id='eth']/tbody/tr[5]/td[2]").text
            # [Max LAN]
            global lan_max
            table = driver.find_element_by_xpath("//table[2]")  # 檢視Network Information欄的xpath可以得知是table[2]
            for tr in table.find_elements_by_tag_name('tr'):
                lan_max = lan_max+1
            lan_max = lan_max/2
            # [Info]
            '''
            global info
            info = []
            for i in range(1, lan_max+1):
                temp = driver.find_element_by_xpath("/html/body/table/tbody/tr/td/table[3]/tbody/tr["+ str(i*2) +"]/td[3]").text.strip()
                info.append(temp)
                temp = driver.find_element_by_xpath("/html/body/table/tbody/tr/td/table[3]/tbody/tr["+ str(i*2+1) +"]/td[2]").text.strip()
                info.append(temp)
            '''
            print model
            print kernel_ver
            print ap_ver
            print lan1_mac
            print lan1_ip
            print lan2_mac
            print lan2_ip
            print lan_max
                    
            if model == None:
                sock.send('Model none.'+"\r\n")
            else:
                sock.send('Model '+ model + "\r\n")
            if kernel_ver == None and ap_ver == None:
                sock.send('Version none.'+"\r\n")
            else:
                sock.send('Kernel '+ kernel_ver+',AP '+ ap_ver+"\r\n")
            if lan1_mac == None and lan1_ip == None:
                sock.send('Lan1 none.'+"\r\n")
            else:
                sock.send('Lan1 MAC '+ lan1_mac+',IP '+ lan1_ip+"\r\n")
            if lan2_mac == None and lan2_ip == None:
                sock.send('Lan2 none.'+ "\r\n")
            else:
                sock.send('Lan2 MAC '+ lan2_mac+',IP '+ lan2_ip+"\r\n")
            if lan_max == -1:
                sock.send('Total lan none.'+"\r\n")
            else:
                sock.send('Total lan '+str(lan_max)+ "\r\n")
        
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
