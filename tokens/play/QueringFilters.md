

# Filtering and inverses

## Requirements

* ? ShowUserSettings(pass, user, path)
  * ✓ What pass.users are allowed? 
  * ✓ Does it need authorized user?
  * ✓ Is specified user enought?
  * ✓ What are all restrictions and requirements?
  * ✓ Does it need user with properties?:
    *  name is default property
    *  reader is role property
    *  writer is other property
    *  department (= engeenering)
    *  manager (=true)
    *  group admin
    *  admin
    *  varified
  * ? Alternatives, tree hierarchy
  * ? Connections to atomic policies beside string descriptions
  * ? Investigation of partially filled parameters
      

* curry ( ShowUserSettingsPolicy(pass, user = "mike", path) )
  ⇒ MikeUserSettingsPolicy(pass, path)
  *  → ShowAllowedValuesOf(argName = pass)
  *  → ShowAllowedValuesOf(argName = path)
  *  → ShowRequirementsOf(argName = pass)
  *  → ShowRequirementsOf(argName = path)

* curry ( ShowUserSettingsPolicy(pass, user = f(x), path) )
  ⇒ FxUserSettingsPolicy(pass, path)

* curry ( ShowUserSettingsPolicy( z(x) → (zPass, zUser, zPath) ) )
  ⇒ UserSettingsPolicy(pass = zPass, user = zUser, path = zPath)



* filter(pass, user, path) :: pass → user → path → bool

  pass → user → path → bool ([user]) 

* filter    :: user → bool
  
  user → bool ([user]) = [user]

* filter    :: pass → user → bool
  
  pass → user → bool ([pass]) = [user → bool] ([user]) =inv [user]

* which users matches?: [user] → filter → [user]
  which of those users matches other criteria?


## Possible sollutins


* partial (mixed) answer:
  * functions descriptions
  * one parameter functions inversions:

    [user].filter(user → filter(user) == true) = [user]
  
  * extraction of one parameter functions
  * ? similar procedure for other parameters
    * ⇒ problem with big input sets... (e.g. every double number)
    * ⇒ combinations, or chain application of such procedure
      produces even bigger input sets... 😏

* but... lets try to do it 😃:
  * ⇒ one parameter filter:
    * ⇒ have inverse:
      * ⇒ generic or
      * ⇒ function
    * ⇒ or have description
    * ⇒ or have antiimage for true values
  * ⇒ two parameters filter:

* filter :: pass → user → path → bool
  
  ([pass]) → [filter_pass(user, path)]
  
  ([pass, user, path]) -filtering-> ([pass, user, path]) -projection-> [pass]


* apply ignoring other parameters?????
  * ⇒ no problem for compound (and or) and not mixing functions, 
    just assume f(other1, other2) is true
    * ⇒ but how would look like fallback??
  * ⇒ What with f(arg1, arg2, ... , argN) ?
  * ⇒ (automatic) 
      
      IteratedAntyImage :: ([arg1], [arg2], ... , [argN]) → [arg1, arg2, ... , argN] 
      
      * ⇒ need to check for all possible (pass, user) 
        combinations (this is inverse for true value) 
    * ⇒ (optional) 
      
      AntyImage :: ([arg1], [arg2], ... , [argN]) → [args] 
      
      * ⇒ implement inverse e.g. for:
        * ⇒ filter(pass, user) ≡ pass = user, 
        * ⇒ filter⁻¹([pass]×[pass], true) ≡ [pass, pass]
    * ⇒ (mandatory)
      
      SymbolicAntyImage :: string 
        
        * ⇒ implement inverse description without computing
          matching arguments

* ⇒ By default arguments are supported as enumerables,
  but what if ranges could be specified?
  * ⇒ this could be optional with fallback to SymbolicAntyImage
    and used if available/feasable

Probably quering filter is totally predictible (static).
If hammer for all nails is needed then quering
must be **Adaptive** (which neccessates dynamic) 
to possible situations


## Examples

* Example: system of equations can be thought as filter:
  * ⇒ f(x,y) = x + y == 0 && 2*x + y == 4 → bool
  * ⇒ f⁻¹(true) = {(4, -4)}
  * ⇒ if this function would be periodic there could be
    infinite number of answers
    * ⇒ inverse image can be also represented as set generator function 
      
      e.g. for f periodic in x (with T = 4) above function:
      f⁻¹(i, true) = (i*4, -4)
      with antiimage: {..., (-4, -4), (0, -4), (4, -4), ...}
    * ⇒ and it is ok, but at what element start iterating
      for example
    * ⇒ so specifying some ranges is **very** practical:
      * ⇒ f⁻¹(XY, true) = f⁻¹(i, true) ∈ XY ⇒ {(4, -4)}
                                      ∉ XY ⇒ ∅


## Cases

* ⇒ in simplest case filter can be only user dependent
  
  f :: X → Bool
  
  then to get all conforming users one may:
  * ⇒ check all provided values:
    
    f⁻¹([x], true) = x : f(x) == true

  * ⇒ know answer generator and only check if value is in range:
    
    f⁻¹([x], true) = [x] ∩ {i: 2i}
    
    f⁻¹([x], true) = [x] ∩ x == 5
    
    * ⇒ this is usefull if answer set is small, 
      e.g. there is unique answer for all
      possible inputs, then 
      f⁻¹(true) = 5
  * ⇒ filter lows:
    * ⇒ right inverse: f(f⁻¹(true)) ≡ true
    * ⇒ for inversing from left: f⁻¹(f(x)) ≡ X|true ∨ X|false
    * ⇒ f⁻¹(X|true, true) = X|true
    * ⇒ idempotence of inverse applications: 
      * ⇒ f⁻¹(f⁻¹(X, true), true) = f⁻¹(X, true)
* ⇒ more complicated situation is when filter
  depends on user and somthing other:
  
  f :: X → Y → Bool
  
  then to get all conforming users one may:
  
  * ⇒ iterate all possibilities and project to user:
    * ⇒ f⁻¹(X,Y, true) = { (x ∈ X, y ∈ Y) : f(x,y) == true }
  * ⇒ curry (parametrize) to get function of one variable:
    * ⇒ f𝐲 :: X → Bool
    * ⇒ f𝐱 :: Y → Bool
    * ⇒ f  :: X → (Y → Bool) = X → f𝐱
    * ⇒ this would return functions taking other parameters 
    * ⇒ f⁻¹(X, true) = { (x ∈ X, f𝐱 ∈ Y → Bool): {f𝐱(Y) == true} ∉ ∅}
  * ⇒ multi-curry is similar as curry:
    * ⇒ f𝐲𝐱 :: () → X → Y → Bool
    * ⇒ f⁻¹(true) = { (f𝐲𝐱): x ∈ X and y ∈ Y }
* ⇒ filter can depend also on same type multiple times:
  
  f :: X → X → Bool
  
  procedure is same as for different types.
* ⇒ filter can depend on many arguments:
  
  f :: X → Y → Z → U → V → Bool
  
  * ⇒ if one searches partial answer, as for exemple
    what properties must have particular X, curring is only option
  * ⇒ curring by one argument can be realized:
    * ⇒ pointing position of argument to curry over
    * ⇒ or pointing name of argument to curry over
  * ⇒ if curring produces function also supporting curring
    effect of curring over multiple arguments may be
    realized
  * ⇒ f𝐱 :: X → (Y → Z → U → V → Bool)
  * ⇒ f𝐱⁻¹(X, true) = { (x ∈ X, f𝐱 ∈ Y → Z → U → V → Bool): {f𝐱(Y,Z,U,V) == true} ∉ ∅}
  * ⇒ f𝐱𝐲 :: X → Y → (Z → U → V → Bool)
  * ⇒ f𝐱𝐲⁻¹(X, Y, true) = { (x ∈ X, y ∈ Y, f𝐱𝐲 ∈ Z → U → V → Bool): {f𝐱(Z,U,V) == true} ∉ ∅}
* ⇒ normally curried filter gives as nothing, problem is unsolved
  but wiritten differently, but if f is composed from simpler
  filters sometimes simplifications can be deduced

## Filter composition curring and reduction

Filter composition and composed filter curring and filter reduction:


What if argument is not point(s) but some range (filter):

* ⇒ this is interesting, but for second version of algorithm!!!
  * ⇒ may be implemented as dyscriminated type union
  * ⇒ or dynamically registered (range) types interpreters for
    particular filters (dynamic cases in filters)
  * ⇒ either way it will be dynamic and ugly, but language
    is gilty for not providing such typing support
  * ⇒ trying to make it beautiful is wast of time,
    with existing language at most it can look beautiful
    but uglyness eventually will show up
  * ⇒ so forget about compiler types, just name types yourself,
    type will mean very little
* ⇒ e.g. imput is int or range [a, b] which can generate ints
  in this range? ...
* ⇒ Answer as subsets generators


Trees:

* ⇒ If policies are trees, trees can be rebalanced.
* ⇒ It is very important to note that policy is subject,
  user, uris, apis are additional things in row problem.
* ⇒ Solution1 is to have [route, policies: (policy, ..., user policy, ...)]
  because it is natural to specify resource security
  as resource property
  * ⇒ it iterate all routes to find requirement for particular user,
    then construct user routes with restrictions:
    * ⇒ [user].filter(route.policies) = [allowed user for route] 
    * ⇒ route.policies.first(p => p == user policy).GetUsers() = [allowed user for route] 
  * ⇒ it is easely understandable what access control resource have:
    * ⇒ Solution1(route) = [policy]
  * ⇒ policy can be specified per resource
  * ⇒ so this is something as route server
* ⇒ Solution2 is to have [user policy, route, [policy]]
  so it is user routes with additional policies
  * ⇒ it is easely understandable what user have access to:
    * ⇒ Solution2(user) = [user policy, route, policy]
  * ⇒ only drowback is that policy must be specified per user-resource
    (no resource(s), only user-resource(s) !!!! )
  * ⇒ so this is something as user server


