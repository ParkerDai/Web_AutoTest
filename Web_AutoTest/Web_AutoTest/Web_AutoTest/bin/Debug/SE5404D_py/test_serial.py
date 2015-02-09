# -*- coding: utf-8 -*-
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import Select
from selenium.common.exceptions import NoSuchElementException
from selenium.common.exceptions import NoAlertPresentException
import unittest, time, re

class Test(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        self.driver.implicitly_wait(30)
        self.base_url = "http://192.168.1.3/"
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_(self):
        driver = self.driver
        # [Serial]
        driver.get(self.base_url + "/serial.asp")
        # [COM 1]
        # ERROR: Caught exception [ERROR: Unsupported command [selectFrame | left | ]]
        driver.find_element_by_link_text("COM 1").click()
        # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rbottom | ]]
        # [TCP Server]
        driver.find_element_by_name("mode").click()
        # ERROR: Caught exception [Error: Dom locators are not implemented yet!]
        driver.find_element_by_css_selector("strong > input[name=\"mode\"]").click()
        driver.find_element_by_name("mode").click()
        # [Mode]
        Select(driver.find_element_by_name("runmode")).select_by_visible_text("RAW")
        # [Max. Connections]
        Select(driver.find_element_by_name("max_conn")).select_by_visible_text("1")
        # [Response Behavior]
        driver.find_element_by_name("tran_mode_server").click()
        # [Accessible IP]
        
        # [Local Port]
        # ERROR: Caught exception [Error: Dom locators are not implemented yet!]
        driver.find_element_by_name("localport").clear()
        driver.find_element_by_name("localport").send_keys("4661")
        # [Apply to all serial ports (Local Port will be enumerated automatically.)]
        driver.find_element_by_name("server_app").click()
        # [UART Mode]
        driver.find_element_by_name("rad_uartmode").click()
        # [Baud Rate]
        Select(driver.find_element_by_id("id_cbbaudrate")).select_by_visible_text("115200")
        # [Parity]
        driver.find_element_by_name("rad_parity").click()
        # [Data bits]
        # ERROR: Caught exception [Error: Dom locators are not implemented yet!]
        
        # [Stop bits]
        driver.find_element_by_name("rad_stopbit").click()
        # [Flow Control]
        driver.find_element_by_name("rad_flow").click()
        # [Apply to all serial ports]
        driver.find_element_by_name("serial_app").click()
        # [Sace Configuration] Button
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
