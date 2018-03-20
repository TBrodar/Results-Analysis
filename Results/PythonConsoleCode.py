try:
    import statsmodels.api as sm
except :
    print("Error: instal statsmodels.api in python \r\n", end="") 
            
alive = True

while alive == True:
    con = input()
    if (con == "run OLS"):
        
        xstr = input()
        ystr = input()
        
        fx = [float(x) for x in xstr.split("|")]
        fy = [float(y) for y in ystr.split("|")]
        
        fxp = sm.add_constant(fx)
         
        mymodel = sm.OLS(fy,fxp)
        res = mymodel.fit() 
        
        print(str(res.params[0])+"|"+str(res.params[1]) + " \r\n", end="") 
        print(str(res.bse[0])+"|"+str(res.bse[1]) + " \r\n", end="")
    else :
        alive = False
        print("Exited \r\n ", end="")
        break