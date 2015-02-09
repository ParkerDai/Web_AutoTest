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

class EmailSendTestMail(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        self.driver.set_page_load_timeout(5)
        self.driver.maximize_window()
        self.driver.implicitly_wait(30)
        self.base_url = "http://"+account+":"+password+"@"+dut_ip
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_email_send_test_mail(self):
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            # Connect the socket to the port where the server is listening
            sock.connect((Host, Port))
            #print >>sys.stderr, 'connecting to %s port %s' % server_address
        except socket.error:
            print 'Tcp connect failed'
            traceback.print_exc()
            time.sleep(1)
        
        try:
            driver = self.driver
            driver.get(self.base_url + "/email.asp")
            # ERROR: Caught exception [ERROR: Unsupported command [selectFrame | left | ]]
            # switch_to
            driver.switch_to_frame("left")
            driver.find_element_by_link_text("E-mail").click()
            # switch_to
            # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rbottom | ]]
            driver.switch_to_default_content()
            driver.switch_to_frame("rbottom")
            # [Sender's E-mail address]
            driver.find_element_by_name("sender_name").clear()
            driver.find_element_by_name("sender_name").send_keys("test@hinet.net")
            # [Receiver's E-mail address 1]
            driver.find_element_by_name("add1_name").clear()
            driver.find_element_by_name("add1_name").send_keys("parkerdai@atop.com.tw")
            # [Receiver's E-mail address 2]
            driver.find_element_by_name("add2_name").clear()
            driver.find_element_by_name("add2_name").send_keys("")
            # [Receiver's E-mail address 3]
            driver.find_element_by_name("add3_name").clear()
            driver.find_element_by_name("add3_name").send_keys("")
            # [Receiver's E-mail address 4]
            driver.find_element_by_name("add4_name").clear()
            driver.find_element_by_name("add4_name").send_keys("")
            # [Receiver's E-mail address 5]
            driver.find_element_by_name("add5_name").clear()
            driver.find_element_by_name("add5_name").send_keys("")
            # [Mail Server]
            driver.find_element_by_name("mail_serv").clear()
            #driver.find_element_by_name("mail_serv").send_keys("www.hibox.hinet.net")
            driver.find_element_by_name("mail_serv").send_keys("ms2.hinet.net")
            #driver.find_element_by_name("mail_serv").send_keys("smtp.gmail.com")
            # [Mail server authentication required.]
            if driver.find_element_by_name("mail_auth").is_selected():
                driver.find_element_by_name("mail_auth").click()
            '''
            if not driver.find_element_by_name("mail_auth").is_selected():
                driver.find_element_by_name("mail_auth").click()
                # [User name]
                driver.find_element_by_name("mail_user").clear()
                driver.find_element_by_name("mail_user").send_keys("")
                # [Password]
                driver.find_element_by_name("mail_pass").clear()
                driver.find_element_by_name("mail_pass").send_keys("")
            '''
            # [Save Configuration]
            driver.find_element_by_name("save").click()
            '''
            # switch_to
            driver.switch_to_default_content()
            driver.switch_to_frame("left")
            driver.find_element_by_link_text("E-mail").click()
            # switch_to
            # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rbottom | ]]
            driver.switch_to_default_content()
            driver.switch_to_frame("rbottom")
            # [Send Test Mail]
            driver.find_element_by_name("send").click()
            time.sleep(2)
            self.assertEqual("Test sending mail successed.", self.close_alert_and_get_its_text())
            '''
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
