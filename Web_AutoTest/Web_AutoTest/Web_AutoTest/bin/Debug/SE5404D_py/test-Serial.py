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
import socket
import sys

dut_ip = ''
account = ''
password = ''
toall = ''
mode = ''
max_connections = ''
response_behavior = ''
accessible_ip = ''
local_port = ''
uart_mode = ''
baudrate = ''
parity = ''
data_bits = ''
stop_bits = ''
flow_control = ''

i = 0
f = open('web_ip.txt', 'r')
for line in f:
    print i
    print line.strip()
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

class TestSerial(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Firefox()
        #self.driver.maximize_window()
        self.driver.implicitly_wait(15)
        self.base_url = "http://"+account+":"+password+"@"+dut_ip
        self.verificationErrors = []
        self.accept_next_alert = True
    
    def test_serial(self):
        try:
            driver = self.driver
            driver.get(self.base_url + "/serial.asp")
            #driver.switch_to_frame("left")
            #driver.find_element_by_name("Image3").click()
            time.sleep(1)
            driver.switch_to_frame("left")
            '''
            if toall.upper() != 'ALL':  # 指定一個COM做設定
                driver.find_element_by_link_text("COM " + toall).click()
            else:   # 藉由COM1來設定全部
                driver.find_element_by_link_text("COM 1").click()
            time.sleep(1)
            '''
	    driver.switch_to_default_content()
	    driver.switch_to_frame("rbottom")
	except:
	    print 'login failed'

        # 網路保持連線
        while True:
            # Create a TCP/IP socket
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            try:
                # Connect the socket to the port where the server is listening
                host = socket.gethostname()
		server_address = (host, 12345)
                sock.connect(server_address)
                #print >>sys.stderr, 'connecting to %s port %s' % server_address
            except socket.error:
                print 'Connect failed'
		traceback.print_exc()
                time.sleep(1)
                continue

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
        def link_mode(settings):

            # Tcp Server radio
            driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/table/tbody/tr[2]/td/p/font/input").click()
            time.sleep(1)
        '''
        def seriel_settings(settings):
            prm = settings.split(' ')
	    uart_mode = prm[0]
	    baudrate = prm[1]
	    parity = prm[2]
	    data_bits = prm[3]
	    stop_bits = prm[4]
	    flow_control = prm[5]
            # UART Mode
            try:
                if uart_mode.upper() == 'RS232':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[2]/td[2]/input").click()
                elif uart_mode.upper() == 'RS422':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[2]/td[2]/input[2]").click()
                elif uart_mode.upper() == 'RS485(2-WIRE)':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[2]/td[2]/input[3]").click()
                elif uart_mode.upper() == 'RS485(4-WIRE)':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[2]/td[2]/input[4]").click()
                else:
                    print 'UART Mode unknown'
            except:
                print 'UART Mode error'
            # Baud rate
            try:
                Select(driver.find_element_by_name("cbbaudrate")).select_by_visible_text(baudrate)
            except:
                print 'Baud rate error'
            # Parity
            try:
                if parity.lower() == 'none' or parity.lower().find('n') != -1:
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[4]/td[2]/input").click()
                elif parity.lower() == 'odd' or parity.lower().find('o') != -1:
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[4]/td[2]/input[2]").click()
                elif parity.lower() == 'even' or parity.lower().find('e') != -1:
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[4]/td[2]/input[3]").click()
                elif parity.lower() == 'mark' or parity.lower().find('m') != -1:
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[4]/td[2]/input[4]").click()
                elif parity.lower() == 'space' or parity.lower().find('s') != -1:
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[4]/td[2]/input[5]").click()
                else:
                    print 'Parity unknown'
            except:
                print 'Parity error'
            # Data bits
            try:
                if data_bits == '8':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[5]/td[2]/input[4]").click()
                elif data_bits == '7':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[5]/td[2]/input[3]").click()
                elif data_bits == '6':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[5]/td[2]/input[2]").click()
                elif data_bits == '5':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[5]/td[2]/input").click()
                else:
                    print 'Data bits unknown'
            except:
                print 'Data bits error'
	        # Stop bits
            try:
                if stop_bits == '1':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[6]/td[2]/input").click()
                elif stop_bits == '2':
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[6]/td[2]/input[2]").click()
                else:
                    print 'Stop bits unknown'
            except:
                print 'Stop bits error'
	    # Flow Control
                if len(flow_control) > 1:
                    fc = flow_control.split(' ')
            try:
                if fc[0].upper() == 'NONE' or fc[0].lower().find('n') != -1:
                    driver.find_element_by_xpath("/html/body/form/table[2]/tbody/tr/td/b[2]/div[4]/table[2]/tbody/tr[7]/td[2]/input").click()
                elif fc[0].lower().find('x') != -1 or fc[0].upper() == 'XON/XOFF':
                    if len(fc) != 4:
                        print 'Flow Control error: len error'
                    if driver.find_element_by_xpath("/html/body/form/table/tbody/tr/td/b[2]/div[4]/font[2]/table/tbody/tr[7]/td[2]/input[2]").get_attribute("disabled") == 'true':
                        pass
                    driver.find_element_by_xpath("/html/body/form/table/tbody/tr/td/b[2]/div[4]/font[2]/table/tbody/tr[7]/td[2]/input[2]").click()
                    time.sleep(1)
                    driver.find_element_by_name("Xon_char").clear()
                    driver.find_element_by_name("Xon_char").send_keys(fc[1])
                    driver.find_element_by_name("Xoff_char").clear()
                    driver.find_element_by_name("Xoff_char").send_keys(fc[2])
                    if fc[3] == 'on':
                        if driver.find_element_by_name("permit_control").get_attribute("value") == '0':
                            driver.find_element_by_name("permit_control").click()
                    elif fc[3] == 'off':
                        if driver.find_element_by_name("permit_control").get_attribute("value") == '1':
                            driver.find_element_by_name("permit_control").click()
                    else:
                        pass
                elif fc[0].lower().find('r') != -1 or fc[0].upper() == 'RTS/CTS':
                    if driver.find_element_by_xpath("/html/body/form/table/tbody/tr/td/b[2]/div[4]/font[2]/table/tbody/tr[7]/td[2]/input[3]").get_attribute("disabled") == 'true':
                        pass
                    driver.find_element_by_xpath("/html/body/form/table/tbody/tr/td/b[2]/div[4]/font[2]/table/tbody/tr[7]/td[2]/input[3]").click()
                    time.sleep(1)
                else:
                    print 'Flow Control unknown'
            except:
                print 'Flow Control error'
            # Serial Settings - Apply to all serial ports 
	    if toall == 'all':
		driver.find_element_by_name("serial_app").click()
	    # Save Configuration
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
