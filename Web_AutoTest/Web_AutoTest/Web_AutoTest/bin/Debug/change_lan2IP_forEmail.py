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
import subprocess # ping

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

class Network(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        self.driver.set_page_load_timeout(5)
        self.driver.maximize_window()
        self.driver.implicitly_wait(30)
        self.base_url = "http://"+account+":"+password+"@"+dut_ip
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_network(self):
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
            driver.get(self.base_url + "/network.asp")
            # ERROR: Caught exception [ERROR: Unsupported command [selectFrame | left | ]]
            # switch_to
            driver.switch_to_frame("left")
            driver.find_element_by_name("Image2").click()
            # ERROR: Caught exception [ERROR: Unsupported command [selectWindow | name=rbottom | ]]
            # switch_to
            driver.switch_to_default_content()
            driver.switch_to_frame("rbottom")
            # [LAN Mode Status]
            # ERROR: Caught exception [Error: Dom locators are not implemented yet!]
            driver.find_element_by_xpath("/html/body/form/table/tbody/tr/td/table[3]/tbody/tr[2]/td[2]/input").click()   # Dual Subnet Mode 
            #driver.find_element_by_xpath("/html/body/form/table/tbody/tr/td/table[3]/tbody/tr[2]/td[2]/input[2]").click() # Redundancy Mode
            '''
            # LAN1 settings
            # [DHCP]
            driver.find_element_by_name("ckDHCP").click()
            driver.find_element_by_name("ckDHCP").click()
            # [IP Address]
            driver.find_element_by_name("editIP1").clear()
            driver.find_element_by_name("editIP1").send_keys("10")
            driver.find_element_by_name("editIP2").clear()
            driver.find_element_by_name("editIP2").send_keys("0")
            driver.find_element_by_name("editIP3").clear()
            driver.find_element_by_name("editIP3").send_keys("50")
            driver.find_element_by_name("editIP4").clear()
            driver.find_element_by_name("editIP4").send_keys("105")
            # [Subnet Mask]
            driver.find_element_by_name("editMASK1").clear()
            driver.find_element_by_name("editMASK1").send_keys("255")
            driver.find_element_by_name("editMASK2").clear()
            driver.find_element_by_name("editMASK2").send_keys("255")
            driver.find_element_by_name("editMASK3").clear()
            driver.find_element_by_name("editMASK3").send_keys("0")
            driver.find_element_by_name("editMASK4").clear()
            driver.find_element_by_name("editMASK4").send_keys("0")
            # [Default Gateway]
            driver.find_element_by_name("editGW1").clear()
            driver.find_element_by_name("editGW1").send_keys("10")
            driver.find_element_by_name("editGW2").clear()
            driver.find_element_by_name("editGW2").send_keys("0")
            driver.find_element_by_name("editGW3").clear()
            driver.find_element_by_name("editGW3").send_keys("0")
            driver.find_element_by_name("editGW4").clear()
            driver.find_element_by_name("editGW4").send_keys("254")
            # [ARP Announce]
            driver.find_element_by_name("editARP1").clear()
            driver.find_element_by_name("editARP1").send_keys("10")
            '''
            ping_ip = dut_ip
            # LAN2 settings: if LAN Mode Status Dual Subnet Mode is select
            if driver.find_element_by_xpath("/html/body/form/table/tbody/tr/td/table[3]/tbody/tr[2]/td[2]/input").is_selected():
                '''
                # [DHCP]
                driver.find_element_by_name("ckDHCP_lan2").click()
                driver.find_element_by_name("ckDHCP_lan2").click()
                # [IP Address]
                driver.find_element_by_name("lan2_editIP1").clear()
                driver.find_element_by_name("lan2_editIP1").send_keys("192")
                driver.find_element_by_name("lan2_editIP2").clear()
                driver.find_element_by_name("lan2_editIP2").send_keys("168")
                driver.find_element_by_name("lan2_editIP3").clear()
                driver.find_element_by_name("lan2_editIP3").send_keys("1")
                '''
                will_be_changed = driver.find_element_by_name("lan2_editIP4").get_attribute("value")
                changed_value = int(will_be_changed) + 1
                driver.find_element_by_name("lan2_editIP4").clear()
                driver.find_element_by_name("lan2_editIP4").send_keys(changed_value)
                '''
                # [Subnet Mask]
                driver.find_element_by_name("lan2_editMASK1").clear()
                driver.find_element_by_name("lan2_editMASK1").send_keys("255")
                driver.find_element_by_name("lan2_editMASK2").clear()
                driver.find_element_by_name("lan2_editMASK2").send_keys("255")
                driver.find_element_by_name("lan2_editMASK3").clear()
                driver.find_element_by_name("lan2_editMASK3").send_keys("255")
                driver.find_element_by_name("lan2_editMASK4").clear()
                driver.find_element_by_name("lan2_editMASK4").send_keys("0")
                # [Default Gateway]
                driver.find_element_by_name("lan2_editGW1").clear()
                driver.find_element_by_name("lan2_editGW1").send_keys("192")
                driver.find_element_by_name("lan2_editGW2").clear()
                driver.find_element_by_name("lan2_editGW2").send_keys("168")
                driver.find_element_by_name("lan2_editGW3").clear()
                driver.find_element_by_name("lan2_editGW3").send_keys("1")
                driver.find_element_by_name("lan2_editGW4").clear()
                driver.find_element_by_name("lan2_editGW4").send_keys("254")
                # [ARP Annpunce]
                driver.find_element_by_name("editARP2").clear()
                driver.find_element_by_name("editARP2").send_keys("10")
            # [Default Gateway Select]
            driver.find_element_by_id("dfgwayval2").click() # LAN1
            driver.find_element_by_id("dfgwayval1").click() # LAN2
            # [DNS 1]
            driver.find_element_by_name("editDNS1_1").clear()
            driver.find_element_by_name("editDNS1_1").send_keys("8")
            driver.find_element_by_name("editDNS1_2").clear()
            driver.find_element_by_name("editDNS1_2").send_keys("8")
            driver.find_element_by_name("editDNS1_3").clear()
            driver.find_element_by_name("editDNS1_3").send_keys("8")
            driver.find_element_by_name("editDNS1_4").clear()
            driver.find_element_by_name("editDNS1_4").send_keys("8")
            # [DNS 2]
            driver.find_element_by_name("editDNS2_1").clear()
            driver.find_element_by_name("editDNS2_1").send_keys("195")
            driver.find_element_by_name("editDNS2_2").clear()
            driver.find_element_by_name("editDNS2_2").send_keys("68")
            driver.find_element_by_name("editDNS2_3").clear()
            driver.find_element_by_name("editDNS2_3").send_keys("1")
            driver.find_element_by_name("editDNS2_4").clear()
            driver.find_element_by_name("editDNS2_4").send_keys("1")
            # SNMP
            # [StsName]
            driver.find_element_by_name("snmp_name").clear()
            driver.find_element_by_name("snmp_name").send_keys("0060E9-686886")
            # [SysLocation]
            driver.find_element_by_name("snmp_local").clear()
            driver.find_element_by_name("snmp_local").send_keys("location")
            # [SysContact]
            driver.find_element_by_name("snmp_contact").clear()
            driver.find_element_by_name("snmp_contact").send_keys("contact")
            # [SNMP]
            driver.find_element_by_name("cksnmp").click()
            #driver.find_element_by_name("cksnmp").click()
            if driver.find_element_by_name("cksnmp").is_selected():
                # [Read Community]
                driver.find_element_by_name("snmp_read_comm").clear()
                driver.find_element_by_name("snmp_read_comm").send_keys("public")
                # [Write Community]
                driver.find_element_by_name("snmp_writ_comm").clear()
                driver.find_element_by_name("snmp_writ_comm").send_keys("private")
                # [SNMP Trap Server]
                driver.find_element_by_name("snmp_serv_1").clear()
                driver.find_element_by_name("snmp_serv_1").send_keys("0")
                driver.find_element_by_name("snmp_serv_2").clear()
                driver.find_element_by_name("snmp_serv_2").send_keys("0")
                driver.find_element_by_name("snmp_serv_3").clear()
                driver.find_element_by_name("snmp_serv_3").send_keys("0")
                driver.find_element_by_name("snmp_serv_4").clear()
                driver.find_element_by_name("snmp_serv_4").send_keys("0")
            '''
            # [Save]Button
            driver.find_element_by_name("save").click()
            time.sleep(1)
            driver.find_element_by_link_text("restart").click()
            time.sleep(5)
            output = subprocess.Popen(["ping.exe", "-n", "1", "-w", "1000",ping_ip],stdout = subprocess.PIPE).communicate()[0]
            while('Request timed out' in output):
                output = subprocess.Popen(["ping.exe", "-n", "1", "-w", "1000",ping_ip],stdout = subprocess.PIPE).communicate()[0]
                sock.send(ping_ip+" offline")
                print(ping_ip+" offline")
        
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
