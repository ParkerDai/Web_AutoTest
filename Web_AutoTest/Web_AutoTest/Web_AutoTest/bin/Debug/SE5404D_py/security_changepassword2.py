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

class SecurityChangepassword2(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        self.driver.set_page_load_timeout(5)
        self.driver.maximize_window()
        self.driver.implicitly_wait(30)
        self.base_url = "http://"+account+":"+password+"@"+dut_ip
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_security_changepassword2(self):
        driver = self.driver
        driver.get(self.base_url + "/system.asp")
        # ERROR: Caught exception [ERROR: Unsupported command [selectFrame | left | ]]
        # switch_to
        driver.switch_to_frame("left")
        driver.find_element_by_link_text("Security").click()
        # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rbottom | ]]
        # switch_to
        driver.switch_to_default_content()
        driver.switch_to_frame("rbottom")
        # [Old Password]
        driver.find_element_by_name("txtOldPwd").clear()
        driver.find_element_by_name("txtOldPwd").send_keys("admin")
        # [New Password]
        driver.find_element_by_name("txtNewPwd").clear()
        driver.find_element_by_name("txtNewPwd").send_keys("")
        # [Verified Password]
        driver.find_element_by_name("txtVerPwd").clear()
        driver.find_element_by_name("txtVerPwd").send_keys("")
        # [Save Password]
        driver.find_element_by_name("sPasswd").click()
        # verify "Save Successfuly"
        try: self.assertEqual("Save Successfuly", driver.find_element_by_css_selector("h1").text)
        except AssertionError as e: self.verificationErrors.append(str(e))
    
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
