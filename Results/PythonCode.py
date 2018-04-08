# -*- coding: utf-8 -*-
#!/usr/bin/env python

# u = TVRegDiff( data, iter, alph, u0, scale, ep, dx, plotflag, diagflag );
# 
# Rick Chartrand (rickc@lanl.gov), Apr. 10, 2011
# Please cite Rick Chartrand, "Numerical differentiation of noisy,
# nonsmooth data," ISRN Applied Mathematics, Vol. 2011, Article ID 164564, 
# 2011. 
#
# Inputs:  (First three required; omitting the final N parameters for N < 7
#           or passing in [] results in default values being used.) 
#       data        Vector of data to be differentiated.
#
#       iter        Number of iterations to run the main loop.  A stopping
#                   condition based on the norm of the gradient vector g
#                   below would be an easy modification.  No default value.
#
#       alph        Regularization parameter.  This is the main parameter
#                   to fiddle with.  Start by varying by orders of
#                   magnitude until reasonable results are obtained.  A
#                   value to the nearest power of 10 is usally adequate.
#                   No default value.  Higher values increase
#                   regularization strenght and improve conditioning.
#
#       u0          Initialization of the iteration.  Default value is the
#                   naive derivative (without scaling), of appropriate
#                   length (this being different for the two methods).
#                   Although the solution is theoretically independent of
#                   the intialization, a poor choice can exacerbate
#                   conditioning issues when the linear system is solved.
#
#       scale       'large' or 'small' (case insensitive).  Default is
#                   'small'.  'small' has somewhat better boundary
#                   behavior, but becomes unwieldly for data larger than
#                   1000 entries or so.  'large' has simpler numerics but
#                   is more efficient for large-scale problems.  'large' is
#                   more readily modified for higher-order derivatives,
#                   since the implicit differentiation matrix is square.
#
#       ep          Parameter for avoiding division by zero.  Default value
#                   is 1e-6.  Results should not be very sensitive to the
#                   value.  Larger values improve conditioning and
#                   therefore speed, while smaller values give more
#                   accurate results with sharper jumps.
#
#       dx          Grid spacing, used in the definition of the derivative
#                   operators.  Default is the reciprocal of the data size.
#
#       plotflag    Flag whether to display plot at each iteration.
#                   Default is 1 (yes).  Useful, but adds significant
#                   running time.
#
#       diagflag    Flag whether to display diagnostics at each
#                   iteration.  Default is 1 (yes).  Useful for diagnosing
#                   preconditioning problems.  When tolerance is not met,
#                   an early iterate being best is more worrying than a
#                   large relative residual.
#                   
# Output:
#
#       u           Estimate of the regularized derivative of data.  Due to
#                   different grid assumptions, length( u ) = 
#                   length( data ) + 1 if scale = 'small', otherwise
#                   length( u ) = length( data ).

## Copyright notice:
# Copyright 2010. Los Alamos National Security, LLC. This material
# was produced under U.S. Government contract DE-AC52-06NA25396 for
# Los Alamos National Laboratory, which is operated by Los Alamos
# National Security, LLC, for the U.S. Department of Energy. The
# Government is granted for, itself and others acting on its
# behalf, a paid-up, nonexclusive, irrevocable worldwide license in
# this material to reproduce, prepare derivative works, and perform
# publicly and display publicly. Beginning five (5) years after
# (March 31, 2011) permission to assert copyright was obtained,
# subject to additional five-year worldwide renewals, the
# Government is granted for itself and others acting on its behalf
# a paid-up, nonexclusive, irrevocable worldwide license in this
# material to reproduce, prepare derivative works, distribute
# copies to the public, perform publicly and display publicly, and
# to permit others to do so. NEITHER THE UNITED STATES NOR THE
# UNITED STATES DEPARTMENT OF ENERGY, NOR LOS ALAMOS NATIONAL
# SECURITY, LLC, NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY,
# EXPRESS OR IMPLIED, OR ASSUMES ANY LEGAL LIABILITY OR
# RESPONSIBILITY FOR THE ACCURACY, COMPLETENESS, OR USEFULNESS OF
# ANY INFORMATION, APPARATUS, PRODUCT, OR PROCESS DISCLOSED, OR
# REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED
# RIGHTS. 

## BSD License notice:
# Redistribution and use in source and binary forms, with or without
# modification, are permitted provided that the following conditions
# are met: 
# 
#      Redistributions of source code must retain the above
#      copyright notice, this list of conditions and the following
#      disclaimer.  
#      Redistributions in binary form must reproduce the above
#      copyright notice, this list of conditions and the following
#      disclaimer in the documentation and/or other materials
#      provided with the distribution. 
#      Neither the name of Los Alamos National Security nor the names of its
#      contributors may be used to endorse or promote products
#      derived from this software without specific prior written
#      permission. 
#  
# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
# CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
# INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
# MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
# DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
# CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
# SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
# LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
# USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
# AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
# LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
# ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
# POSSIBILITY OF SUCH DAMAGE. 
#
#########################################################
#                                                       #
# Python translation by Simone Sturniolo                #
# Rutherford Appleton Laboratory, STFC, UK (2014)       #
# simonesturniolo@gmail.com                             #
#                                                       #
#########################################################
 
import sys

try:
    import numpy as np
    from scipy import sparse
    from scipy.sparse import linalg as splin
except ImportError:
    print("Error : Numpy and Scipy must be installed for TVRegDiag to work - aborting.\r\n", end="")
    exit()
 
# Utility function.
def chop( v ):
    return v[1:]

result = False;
 
#TVRegDiff(C2, iterations, alpha, dx=step, ep=divisionByZeroParameter, scale='small')
def TVRegDiff( data, itern, alph, u0=None, scale='small', ep=1e-6, dx=None):
    print("info : (Started4)\r\n", end="") 
    global result
    ## code starts here
    # Make sure we have a column vector
    data = np.array(data)
    if (len(data.shape) != 1):
        print("Error : data is not a column vector.\r\n", end="")
        return
    # Get the data size.
    n = len(data)
    
    # Default checking. (u0 is done separately within each method.)
    if dx is None:
        dx = 1.0 / n
    
    # Different methods for small- and large-scale problems.
    if (scale.lower() == 'small'):
        
        # Construct differentiation matrix.
        c = np.ones(n + 1) / dx
        D = sparse.spdiags( [ -c, c ], [ 0, 1 ], n, n + 1 )
        
        DT = D.transpose()
        
        # Construct antidifferentiation operator and its adjoint.
        A  = lambda x: chop( np.cumsum(x) - 0.5 * (x + x[0]) ) * dx
        AT = lambda w: (sum( w ) * np.ones( n + 1 ) - np.transpose(np.concatenate(([sum( w ) / 2.0], np.cumsum( w ) - w / 2.0) ))) * dx
        
        # Default initialization is naive derivative
        
        if u0 is None:
            u0 = np.concatenate(([0], np.diff(data), [0]))
        
        u = u0
        # Since Au( 0 ) = 0, we need to adjust.
        ofst = data[0]
        # Precompute.
        ATb = AT( ofst - data )        # input: size n
        
        # Main loop.
        for ii in range(1, itern+1):
            # Diagonal matrix of weights, for linearizing E-L equation.
            Q = sparse.spdiags( 1. / ( np.sqrt( ( D * u )**2 + ep ) ), 0, n, n )
            # Linearized diffusion matrix, also approximation of Hessian.
            L = dx * DT * Q * D
            
            # Gradient of functional.
            g = AT( A( u ) ) + ATb + alph * L * u
            
            # Prepare to solve linear equation.
            tol = 1e-4
            maxit = 100
            # Simple preconditioner.
            P = alph * sparse.spdiags(  L.diagonal() + 1, 0, n + 1, n + 1 )
            
            linop = lambda v: ( alph * L * v + AT( A( v ) ) )
            linop = splin.LinearOperator((n + 1, n + 1), linop)

            [s, info_i] = sparse.linalg.cg( linop, g, None, tol, maxit, None, P )
            print("iteration,      relative change,      gradient norm |    {0:4d}                 {1:.3e}              {2:.3e}\r\n".format(ii, np.linalg.norm( s[0] ) / np.linalg.norm( u ), np.linalg.norm( g ) ),  end="")
 
            if info_i == 0 :  
                result = True
             #   break
            # Update solution.
            u = u - s; 
            
    elif (scale.lower() == 'large'):
        # Construct antidifferentiation operator and its adjoint.
        A = lambda v: np.cumsum(v)
        AT = lambda w: ( sum(w) * np.ones( len( w ) ) - np.transpose( np.concatenate(([0.0], np.cumsum( w[:-1] ) )) ) )
        # Construct differentiation matrix.
        c = np.ones( n )
        D = sparse.spdiags( [ -c, c ], [ 0, 1 ], n, n ) / dx
        mask = np.ones((n, n))
        mask[-1, -1] = 0.0
        D = sparse.dia_matrix(D.multiply(mask))
        DT = D.transpose()
        # Since Au( 0 ) = 0, we need to adjust.
        data = data - data[ 0 ]
        # Default initialization is naive derivative.
        if u0 is None:
            u0 = np.concatenate(([ 0], np.diff( data )))
        u = u0
        # Precompute.
        ATd = AT( data )
        # Main loop.
        for ii in range(1, itern + 1):
            # Diagonal matrix of weights, for linearizing E-L equation.
            Q = sparse.spdiags( 1./ np.sqrt( (D*u)**2.0 +  ep ), 0, n, n )
            # Linearized diffusion matrix, also approximation of Hessian.
            L = DT*Q*D
            # Gradient of functional.
            g = AT( A( u ) ) - ATd
            g = g + alph * L * u
            # Build preconditioner.
            c = np.cumsum( range(n, 0, -1))
            B = alph * L + sparse.spdiags( c[::-1], 0, n, n )
            # droptol = 1.0e-2
            R = sparse.dia_matrix(np.linalg.cholesky( B.todense() ))
            # Prepare to solve linear equation.
            tol = 1.0e-4
            maxit = 100
            
            linop = lambda v: ( alph * L * v + AT( A( v ) ) )
            linop = splin.LinearOperator((n, n), linop)
            [s, info_i] = sparse.linalg.cg( linop, -g, None, tol, maxit, None, np.dot(R.transpose(), R) )
            print("iteration,      relative change,      gradient norm |    {0:4d}                 {1:.3e}              {2:.3e}\r\n".format(ii, np.linalg.norm( s[0] ) / np.linalg.norm( u ), np.linalg.norm( g ) ), end="")
  
            if info_i == 0 :
                result = True
              #  break
 
            # Update current solution
            u = u + s 
        
        u = u/dx
    
    return u


def savitzky_golay(y, window_size, order, deriv=0, rate=1):
    r"""Smooth (and optionally differentiate) data with a Savitzky-Golay filter.
    The Savitzky-Golay filter removes high frequency noise from data.
    It has the advantage of preserving the original shape and
    features of the signal better than other types of filtering
    approaches, such as moving averages techniques.
    Parameters
    ----------
    y : array_like, shape (N,)
        the values of the time history of the signal.
    window_size : int
        the length of the window. Must be an odd integer number.
    order : int
        the order of the polynomial used in the filtering.
        Must be less then `window_size` - 1.
    deriv: int
        the order of the derivative to compute (default = 0 means only smoothing)
    Returns
    -------
    ys : ndarray, shape (N)
        the smoothed signal (or it's n-th derivative).
    Notes
    -----
    The Savitzky-Golay is a type of low-pass filter, particularly
    suited for smoothing noisy data. The main idea behind this
    approach is to make for each point a least-square fit with a
    polynomial of high order over a odd-sized window centered at
    the point.
    Examples
    --------
    t = np.linspace(-4, 4, 500)
    y = np.exp( -t**2 ) + np.random.normal(0, 0.05, t.shape)
    ysg = savitzky_golay(y, window_size=31, order=4)
    import matplotlib.pyplot as plt
    plt.plot(t, y, label='Noisy signal')
    plt.plot(t, np.exp(-t**2), 'k', lw=1.5, label='Original signal')
    plt.plot(t, ysg, 'r', label='Filtered signal')
    plt.legend()
    plt.show()
    References
    ----------
    .. [1] A. Savitzky, M. J. E. Golay, Smoothing and Differentiation of
       Data by Simplified Least Squares Procedures. Analytical
       Chemistry, 1964, 36 (8), pp 1627-1639.
    .. [2] Numerical Recipes 3rd Edition: The Art of Scientific Computing
       W.H. Press, S.A. Teukolsky, W.T. Vetterling, B.P. Flannery
       Cambridge University Press ISBN-13: 9780521880688
    """
    import numpy as np
    from math import factorial
    
    try:
        window_size = np.abs(np.int(window_size))
        order = np.abs(np.int(order))
    except ValueError:
        print("Error : window_size and order have to be of type int \r\n", end="")
        exit()
    if window_size % 2 != 1 or window_size < 1:
        print("Error : window_size size must be a positive odd number \r\n", end="")
        exit()
    if window_size < order + 2:
        print("Error : window_size is too small for the polynomials order \r\n", end="")
        exit()
    order_range = range(order+1)
    half_window = (window_size -1) // 2
    # precompute coefficients
    b = np.mat([[k**i for i in order_range] for k in range(-half_window, half_window+1)])
    m = np.linalg.pinv(b).A[deriv] * rate**deriv * factorial(deriv)
    # pad the signal at the extremes with
    # values taken from the signal itself
    firstvals = y[0] - np.abs( y[1:half_window+1][::-1] - y[0] )
    lastvals = y[-1] + np.abs(y[-half_window-1:-1][::-1] - y[-1])
    y = np.concatenate((firstvals, y, lastvals))
    return np.convolve( m[::-1], y, mode='valid')





lines = []

try :
    l = sys.stdin.readline()
    alpha = float(l)
except :
    print("Error : Alpha parameter ("+l+") is not number.\r\n", end="")
    exit()
try :
    l = sys.stdin.readline()
    iterations = int(l)
except :
    print("Error : Max iterations ("+l+") is not integer number.\r\n", end="")
    exit()
try :
    l = sys.stdin.readline()
    divisionByZeroParameter = float(l)
except :
    print("Error : Zero parameter ("+l+") is not number.\r\n", end="")
    exit()
try :
    dataFile = sys.argv[1]
    No3 = open(dataFile, "r")
    lines = No3.readlines()
    No3.close()
except :
    print("Error : Python can't open C-V data file. " + dataFile + "\r\n", end="")
    exit()
try :
    l = sys.stdin.readline()
    smoothW = int(l)
except :
    print("Error : Smooth W CheckBox error. Selected index = "+l+" \r\n", end="")
    exit()
try :
    l = sys.stdin.readline()
    smoothWindow = int(l)
except :
    if smoothW == 1 :
        print("Error : Window points ("+l+") is not integer number.\r\n", end="")
        exit()
try :
    l = sys.stdin.readline()
    smoothPolynom = int(l)
except :
    if smoothW == 1 :
        print("Error : Polynom order ("+l+") is not integer number.\r\n", end="")
        exit()
try :
    l = sys.stdin.readline()
    varEpsilon = float(l)
except :
    print("Error : Material relative permitivity ("+l+") is not number. \r\n", end="")
    exit()
try :
    l = sys.stdin.readline()
    Area = float(l)
except :
    print("Error : Material area (+"+l+") is not number. \r\n", end="")
    exit()
try :
    l = sys.stdin.readline()
    step = float(l)
except :
    print("Error : Voltage step ("+l+") is not number. \r\n", end="")
    exit()
try :
    l = sys.stdin.readline() 
    MMSEOptionValue = int(l)
except :
    print("Error : MMSEOptionValue ("+l+") is not integer number. \r\n", end="")
    exit()
try :
    l = sys.stdin.readline() 
    selectedScale = int(l)
except :
    print("Error : selectedScale ("+l+") is not integer number. \r\n", end="")
    exit()

if len(lines) < 3 :
    print("Error : No data provided.\r\n", end="")
    exit()

#učitavanje iz datoteka
V = []
C = []
C2= []
C2S = []
for line in lines:
    v, c = line.split()
    V += [float(v)]
    C += [1.0/float(c)]
    C2 += [1.0/(float(c)**2)]

W = []
N = []

print("info : (Started)\r\n", end="") 
 
e = 1.60217662e-19
vacuumPermittivity = 8.8541878e-12

if(selectedScale == 1) :
    Scale = 'large'
else :
    Scale = 'small'

# u = TVRegDiff( data, iter, alph, u0, scale, ep, dx, plotflag, diagflag );
print("info : (Started2)\r\n", end="") 
C2D = TVRegDiff(C2, iterations, alpha, dx=step, ep=divisionByZeroParameter, scale=Scale)

C2S += [C2[0]]
print("info : (Started3)\r\n", end="") 
#izračun W i N
if(selectedScale == 1) :
    num = len(C2D)
else :
    num = len(C2D)-1
    
for i in range(num):
    try:
        N += [((1.0e-6*(-2))/(e*varEpsilon*vacuumPermittivity*(Area*1e-6)*(Area*1e-6)))*((1.0)/(C2D[i]*1e24))]
        C2S += [C2S[i] + C2D[i]*step]
    except ZeroDivisionError:
        print("Error : Zero division error (voltage of two successive points is equal).\r\n", end="")
        exit()

C2S = C2S[:-1]  # len(C2) == len(C2S)


if   (MMSEOptionValue == 1) : 
    C2S = [ c  - C2S[-1] + C2[-1] for c in C2S]  
elif (MMSEOptionValue == 2) : 
    deltaC = (1.0/len(C2S))*sum([C2[i] - C2S[i] for i in range(len(C2S))])
    C2S = [ c  + deltaC for c in C2S]  
    
    

if smoothW == 0:
    CS = [np.sqrt(c2) for c2 in C2S] 
elif smoothW == 1 :
    CS = savitzky_golay(np.asarray(C), smoothWindow, smoothPolynom).tolist()
elif smoothW == 2 :
    CS = C 
    
for i in range(num) :
        W += [(varEpsilon*vacuumPermittivity*Area*1e-6*1e6)*(CS[i]*1e12)]
 
s = ""
for i in range(len(N)) :
    s += str(W[i]) + "|" + str(N[i])+ ";"
print("Result NW : " + s[:-1] + "\r\n", end="")
s = ""
for i in range(len(V)) :
    s += str(V[i]) + "%" + str(np.sqrt(1.0/C2S[i]))+ ";"
print("Result CV : " + s[:-1] + "\r\n", end="")

if (result == True) :
    print("Done \r\n ", end="")
else :
    print("NotConverged \r\n ", end="")